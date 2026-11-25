using System.ComponentModel.DataAnnotations;

namespace NavidGelato_Site.Models;

public enum PaymentLinkStatus
{
    Created = 0,
    Opened = 1,
    GatewayReady = 2,
    VerificationPending = 3,
    Paid = 4,
    Failed = 5,
    Expired = 6
}

public sealed class BranchPaymentLink
{
    public long Id { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [MaxLength(64)]
    public string PublicToken { get; set; } = string.Empty;

    [MaxLength(16)]
    public string? ShortCode { get; set; }

    [MaxLength(64)]
    public string ResNum { get; set; } = string.Empty;

    public long Amount { get; set; }

    [MaxLength(26)]
    public string SettlementIban { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? CellNumber { get; set; }

    public PaymentLinkStatus Status { get; set; } = PaymentLinkStatus.Created;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? OpenedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? LastCallbackAtUtc { get; set; }

    [MaxLength(600)]
    public string? GatewayToken { get; set; }

    [MaxLength(64)]
    public string? GatewayRequestId { get; set; }

    public DateTime? GatewayRequestStartedAtUtc { get; set; }

    [MaxLength(160)]
    public string? RefNum { get; set; }

    [MaxLength(100)]
    public string? TraceNo { get; set; }

    public long? VerifiedAmount { get; set; }

    [MaxLength(1200)]
    public string? FailureMessage { get; set; }
}

public sealed class PaymentGatewaySettings
{
    public int Id { get; set; }

    public bool Enabled { get; set; }

    [MaxLength(64)]
    public string TerminalId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string TokenUrl { get; set; } = "https://sep.shaparak.ir/onlinepg/onlinepg";

    [MaxLength(1000)]
    public string PaymentUrlTemplate { get; set; } = "https://sep.shaparak.ir/OnlinePG/SendToken?token={token}";

    [MaxLength(1000)]
    public string VerifyUrl { get; set; } = "https://sep.shaparak.ir/verifyTxnRandomSessionkey/ipg/VerifyTransaction";

    [MaxLength(500)]
    public string? PublicBaseUrl { get; set; }

    [MaxLength(30)]
    public string DefaultPurchaseId { get; set; } = "000000000000000000000000000000";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
