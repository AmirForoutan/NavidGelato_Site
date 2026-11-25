using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Services;

public interface ISepPaymentGateway
{
    Task<SepTokenResult> RequestTokenAsync(
        PaymentGatewaySettings settings,
        BranchPaymentLink payment,
        string iban,
        string callbackUrl,
        CancellationToken cancellationToken);

    Task<SepVerifyResult> VerifyAsync(
        PaymentGatewaySettings settings,
        string refNum,
        CancellationToken cancellationToken);

    string BuildPaymentUrl(PaymentGatewaySettings settings, string token);
}

public sealed record SepTokenResult(
    bool Success,
    string? Token,
    string? ErrorCode,
    string? ErrorDescription);

public sealed record SepVerifyResult(
    bool Success,
    int? ResultCode,
    string? ResultDescription,
    long? Amount,
    string? ResNum,
    string? RefNum);

public sealed class SepPaymentGateway(HttpClient httpClient) : ISepPaymentGateway
{
    public async Task<SepTokenResult> RequestTokenAsync(
        PaymentGatewaySettings settings,
        BranchPaymentLink payment,
        string iban,
        string callbackUrl,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            action = "token",
            TerminalId = settings.TerminalId,
            Amount = payment.Amount,
            ResNum = payment.ResNum,
            RedirectUrl = callbackUrl,
            CellNumber = payment.CellNumber,
            TokenExpiryInMin = 15,
            SettlementIbanInfo = new[]
            {
                new
                {
                    IBAN = iban,
                    Amount = payment.Amount,
                    PurchaseId = settings.DefaultPurchaseId
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync(settings.TokenUrl, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new(false, null, ((int)response.StatusCode).ToString(), Limit(body));

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var token = ReadString(root, "Token");
            var status = ReadInt(root, "Status");
            var errorCode = ReadString(root, "ErrorCode") ?? ReadInt(root, "ErrorCode")?.ToString();
            var errorDescription = ReadString(root, "ErrorDesc") ?? ReadString(root, "ErrorDescription");

            if (!string.IsNullOrWhiteSpace(token) && (!status.HasValue || status.Value == 1))
                return new(true, token, null, null);

            return new(false, null, errorCode ?? status?.ToString(), errorDescription ?? "درگاه توکن پرداخت معتبری برنگرداند.");
        }
        catch (JsonException)
        {
            return new(false, null, null, "پاسخ دریافت توکن از درگاه قابل پردازش نبود.");
        }
    }

    public async Task<SepVerifyResult> VerifyAsync(
        PaymentGatewaySettings settings,
        string refNum,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                object terminalNumber = long.TryParse(settings.TerminalId, out var numericTerminal)
                    ? numericTerminal
                    : settings.TerminalId;

                var payload = new
                {
                    RefNum = refNum,
                    TerminalNumber = terminalNumber
                };

                using var response = await httpClient.PostAsJsonAsync(settings.VerifyUrl, payload, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if ((int)response.StatusCode >= 500 && attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    return new(false, (int)response.StatusCode, Limit(body), null, null, refNum);

                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;
                var success = ReadBool(root, "Success") ?? false;
                var resultCode = ReadInt(root, "ResultCode");
                var resultDescription = ReadString(root, "ResultDescription");

                JsonElement detail = default;
                var hasDetail = TryGetProperty(root, "TransactionDetail", out detail) && detail.ValueKind == JsonValueKind.Object;

                var amount = hasDetail
                    ? ReadLong(detail, "OrginalAmount") ?? ReadLong(detail, "OriginalAmount") ?? ReadLong(detail, "Amount")
                    : ReadLong(root, "OrginalAmount") ?? ReadLong(root, "OriginalAmount") ?? ReadLong(root, "Amount");

                var resNum = hasDetail ? ReadString(detail, "ResNum") : ReadString(root, "ResNum");
                var verifiedRefNum = hasDetail ? ReadString(detail, "RefNum") : ReadString(root, "RefNum");

                var verified = success && (!resultCode.HasValue || resultCode.Value == 0);
                return new(verified, resultCode, resultDescription, amount, resNum, verifiedRefNum ?? refNum);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < 3)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (JsonException)
            {
                return new(false, null, "پاسخ تایید تراکنش از درگاه قابل پردازش نبود.", null, null, refNum);
            }
        }

        return new(false, null, Limit(lastException?.Message) ?? "ارتباط با سرویس تایید تراکنش برقرار نشد.", null, null, refNum);
    }

    public string BuildPaymentUrl(PaymentGatewaySettings settings, string token)
    {
        var encoded = Uri.EscapeDataString(token);
        if (settings.PaymentUrlTemplate.Contains("{token}", StringComparison.OrdinalIgnoreCase))
            return settings.PaymentUrlTemplate.Replace("{token}", encoded, StringComparison.OrdinalIgnoreCase);

        var separator = settings.PaymentUrlTemplate.Contains('?') ? "&" : "?";
        return $"{settings.PaymentUrlTemplate}{separator}token={encoded}";
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number)) return number;
        return null;
    }

    private static long? ReadLong(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out number)) return number;
        return null;
    }

    private static bool? ReadBool(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value)) return null;
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) return value.GetBoolean();
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number != 0;
        if (value.ValueKind == JsonValueKind.String)
        {
            if (bool.TryParse(value.GetString(), out var result)) return result;
            if (int.TryParse(value.GetString(), out number)) return number != 0;
        }
        return null;
    }

    private static string? Limit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= 1000 ? value : value[..1000];
    }
}
