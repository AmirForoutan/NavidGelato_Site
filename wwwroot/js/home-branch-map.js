(() => {
    const root = document.querySelector('[data-home-branch-map]');
    if (!root) return;

    const dataNode = root.querySelector('[data-branch-map-data]');
    const shell = root.querySelector('.home-map-shell');
    const stage = root.querySelector('[data-home-map-stage]');
    const canvas = root.querySelector('[data-home-map-canvas]');
    const svg = root.querySelector('[data-home-map-svg]');
    const world = root.querySelector('[data-home-map-world]');
    const provinceLayer = root.querySelector('[data-home-map-provinces]');
    const markerLayer = root.querySelector('[data-home-map-markers]');
    const loading = root.querySelector('[data-home-map-loading]');
    const info = root.querySelector('[data-home-map-info]');
    const infoName = root.querySelector('[data-home-map-info-name]');
    const infoCity = root.querySelector('[data-home-map-info-city]');
    const infoAddress = root.querySelector('[data-home-map-info-address]');
    const infoLink = root.querySelector('[data-home-map-info-link]');
    const listButtons = [...root.querySelectorAll('[data-home-map-branch]')];

    if (!dataNode || !stage) return;

    let branches = [];
    try {
        branches = JSON.parse(dataNode.textContent || '[]');
    } catch {
        branches = [];
    }

    if (!Array.isArray(branches) || branches.length === 0) return;

    const logoUrl = root.dataset.logoUrl || '';
    const geoUrl = root.dataset.geoUrl || '';
    const tileStatusUrl = root.dataset.tileStatusUrl || '';

    // MapLibre loads vector tiles inside a Web Worker. Root-relative URLs
    // such as "/map/offline/tiles/{z}/{x}/{y}.pbf" do not have a document
    // base there, so Request() rejects them. Convert the template to an
    // absolute public URL before it reaches the worker while preserving
    // MapLibre's {z}/{x}/{y} placeholders.
    const resolveMapTemplateUrl = value => {
        const rawUrl = String(value || '').trim();
        if (!rawUrl) return '';

        const tokens = [];
        const protectedUrl = rawUrl.replace(/\{[^}]+\}/g, token => {
            const placeholder = `__NP_MAP_TOKEN_${tokens.length}__`;
            tokens.push({ placeholder, token });
            return placeholder;
        });

        try {
            let absoluteUrl = new URL(protectedUrl, document.baseURI).href;
            tokens.forEach(({ placeholder, token }) => {
                absoluteUrl = absoluteUrl.replace(placeholder, token);
            });
            return absoluteUrl;
        } catch {
            return rawUrl;
        }
    };

    const tileUrl = resolveMapTemplateUrl(root.dataset.tileUrl || '');

    const normalizeBranchCoordinates = branch => {
        let lat = Number(branch?.lat);
        let lng = Number(branch?.lng);

        if (!Number.isFinite(lat) || !Number.isFinite(lng))
            return null;

        const isInsideIran = (latitude, longitude) =>
            latitude >= 24.2 &&
            latitude <= 40.9 &&
            longitude >= 43.5 &&
            longitude <= 64.2;

        if (!isInsideIran(lat, lng) && isInsideIran(lng, lat)) {
            [lat, lng] = [lng, lat];
        }

        if (!isInsideIran(lat, lng))
            return null;

        return { lat, lng };
    };

    const setActiveUi = branch => {
        if (!branch) return;

        listButtons.forEach(button => {
            button.classList.toggle(
                'is-active',
                button.dataset.homeMapBranch === String(branch.id)
            );
        });

        if (info && infoName && infoCity && infoAddress && infoLink) {
            infoName.textContent = branch.name || 'شعبه نوید ژلاتو';
            infoCity.textContent = branch.city || '';
            infoAddress.textContent = branch.address || '';
            infoLink.href = branch.detailsUrl || '#';
            info.classList.add('is-visible');
        }
    };

    const branchById = id =>
        branches.find(branch => String(branch.id) === String(id));

    const bindBranchList = activate => {
        listButtons.forEach(button => {
            button.onclick = () => {
                const branch = branchById(button.dataset.homeMapBranch);
                if (branch) activate(branch);
            };
        });
    };

    const showLoading = text => {
        if (!loading) return;
        loading.textContent = text;
        loading.classList.remove('is-hidden');
    };

    const hideLoading = () => loading?.classList.add('is-hidden');

    const vectorStyle = (maxZoom) => ({
        version: 8,
        name: 'NavidPro Offline Shortbread',
        sources: {
            shortbread: {
                type: 'vector',
                tiles: [tileUrl],
                minzoom: 0,
                maxzoom: maxZoom,
                bounds: [44.0, 24.5, 63.5, 40.5],
                attribution: '© OpenStreetMap contributors · Geofabrik'
            }
        },
        layers: [
            {
                id: 'background',
                type: 'background',
                paint: { 'background-color': '#09090b' }
            },
            {
                id: 'ocean',
                type: 'fill',
                source: 'shortbread',
                'source-layer': 'ocean',
                paint: { 'fill-color': '#0d2632' }
            },
            {
                id: 'sites',
                type: 'fill',
                source: 'shortbread',
                'source-layer': 'sites',
                paint: {
                    'fill-color': '#151b17',
                    'fill-opacity': 0.95
                }
            },
            {
                id: 'water-polygons',
                type: 'fill',
                source: 'shortbread',
                'source-layer': 'water_polygons',
                paint: {
                    'fill-color': '#153744',
                    'fill-opacity': 0.95
                }
            },
            {
                id: 'buildings',
                type: 'fill',
                source: 'shortbread',
                'source-layer': 'buildings',
                minzoom: 12,
                paint: {
                    'fill-color': '#29292f',
                    'fill-outline-color': '#34343b',
                    'fill-opacity': 0.9
                }
            },
            {
                id: 'boundaries',
                type: 'line',
                source: 'shortbread',
                'source-layer': 'boundaries',
                paint: {
                    'line-color': '#50383d',
                    'line-width': [
                        'interpolate',
                        ['linear'],
                        ['zoom'],
                        4, 0.7,
                        8, 1.05,
                        13, 1.5
                    ],
                    'line-opacity': 0.8
                }
            },
            {
                id: 'street-casing',
                type: 'line',
                source: 'shortbread',
                'source-layer': 'streets',
                layout: {
                    'line-cap': 'round',
                    'line-join': 'round'
                },
                paint: {
                    'line-color': '#08080a',
                    'line-width': [
                        'interpolate',
                        ['linear'],
                        ['zoom'],
                        5, 1.2,
                        9, 2.2,
                        12, 4.1,
                        15, 7.2
                    ],
                    'line-opacity': 0.8
                }
            },
            {
                id: 'streets',
                type: 'line',
                source: 'shortbread',
                'source-layer': 'streets',
                layout: {
                    'line-cap': 'round',
                    'line-join': 'round'
                },
                paint: {
                    'line-color': [
                        'match',
                        ['get', 'kind'],
                        'motorway', '#b82a39',
                        'trunk', '#9e523f',
                        'primary', '#9a704e',
                        'secondary', '#72675c',
                        'tertiary', '#56565d',
                        'rail', '#4c4c54',
                        '#3a3a42'
                    ],
                    'line-width': [
                        'interpolate',
                        ['linear'],
                        ['zoom'],
                        5, 0.6,
                        9, 1.25,
                        12, 2.7,
                        15, 5.2
                    ],
                    'line-opacity': [
                        'interpolate',
                        ['linear'],
                        ['zoom'],
                        4, 0.65,
                        10, 0.9,
                        14, 1
                    ]
                }
            }
        ]
    });

    const createVectorMarkerElement = branch => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'np-vector-marker';
        button.setAttribute(
            'aria-label',
            `شعبه ${branch.name || ''}، ${branch.city || ''}`
        );

        const pin = document.createElement('span');
        pin.className = 'np-vector-marker-pin';

        if (logoUrl) {
            const image = document.createElement('img');
            image.src = logoUrl;
            image.alt = '';
            pin.appendChild(image);
        } else {
            const fallback = document.createElement('b');
            fallback.textContent = 'NP';
            pin.appendChild(fallback);
        }

        const label = document.createElement('span');
        label.className = 'np-vector-marker-label';
        label.textContent = branch.name || 'نوید ژلاتو';

        button.append(pin, label);
        return button;
    };

    const initVectorMap = async () => {
        if (!canvas || !tileStatusUrl || !tileUrl || !window.maplibregl)
            return false;

        let status;
        try {
            const response = await fetch(tileStatusUrl, {
                cache: 'no-store',
                credentials: 'same-origin'
            });
            if (!response.ok) return false;
            status = await response.json();
        } catch {
            return false;
        }

        if (!status?.available)
            return false;

        showLoading('در حال بارگذاری نقشه خیابانی آفلاین…');

        canvas.hidden = false;
        if (svg) svg.hidden = true;
        shell?.classList.add('vector-ready');

        const maxZoom = Number.isFinite(Number(status.maxZoom))
            ? Math.max(0, Math.min(22, Number(status.maxZoom)))
            : 14;

        let map;
        try {
            map = new window.maplibregl.Map({
                container: canvas,
                style: vectorStyle(maxZoom),
                center: [53.4, 32.2],
                zoom: 4.15,
                minZoom: 3.4,
                maxZoom: Math.max(16, maxZoom + 2),
                maxBounds: [[43.1, 23.2], [64.6, 41.6]],
                attributionControl: false,
                dragRotate: false,
                pitchWithRotate: false,
                cooperativeGestures: false
            });
        } catch {
            canvas.hidden = true;
            if (svg) svg.hidden = false;
            shell?.classList.remove('vector-ready');
            return false;
        }

        let branchLayerReady = false;

        const branchGeoJson = {
            type: 'FeatureCollection',
            features: branches
                .map(branch => {
                    const coordinates = normalizeBranchCoordinates(branch);
                    if (!coordinates) {
                        console.warn(
                            '[NavidPro Map] Invalid branch coordinates:',
                            branch?.id,
                            branch?.name,
                            branch?.lat,
                            branch?.lng
                        );
                        return null;
                    }

                    return {
                        type: 'Feature',
                        id: String(branch.id),
                        properties: {
                            id: String(branch.id),
                            name: branch.name || '',
                            city: branch.city || ''
                        },
                        geometry: {
                            type: 'Point',
                            coordinates: [coordinates.lng, coordinates.lat]
                        }
                    };
                })
                .filter(Boolean)
        };

        const resetVectorView = () => {
            map.fitBounds(
                [[44.0, 24.5], [63.5, 40.5]],
                {
                    padding: {
                        top: 46,
                        right: 46,
                        bottom: 46,
                        left: 46
                    },
                    duration: 650
                }
            );
        };

        const activateVectorBranch = branch => {
            if (!branch) return;

            setActiveUi(branch);

            if (branchLayerReady && map.getLayer('navidpro-branch-active')) {
                map.setFilter(
                    'navidpro-branch-active',
                    ['==', ['get', 'id'], String(branch.id)]
                );
            }

            const coordinates = normalizeBranchCoordinates(branch);
            if (!coordinates) return;

            map.flyTo({
                center: [coordinates.lng, coordinates.lat],
                zoom: Math.max(map.getZoom(), 13.4),
                speed: 1.15,
                curve: 1.35,
                essential: true
            });
        };

        bindBranchList(activateVectorBranch);

        root.querySelector('[data-home-map-zoom-in]')?.addEventListener(
            'click',
            () => map.zoomIn({ duration: 220 })
        );
        root.querySelector('[data-home-map-zoom-out]')?.addEventListener(
            'click',
            () => map.zoomOut({ duration: 220 })
        );
        root.querySelector('[data-home-map-reset]')?.addEventListener(
            'click',
            resetVectorView
        );

        map.addControl(
            new window.maplibregl.AttributionControl({
                compact: true,
                customAttribution: '© OpenStreetMap contributors · Geofabrik'
            }),
            'bottom-left'
        );

        return await new Promise(resolve => {
            let settled = false;

            const finish = value => {
                if (settled) return;
                settled = true;
                resolve(value);
            };

            const timeout = window.setTimeout(() => {
                if (!settled) {
                    try { map.remove(); } catch {}
                    canvas.hidden = true;
                    if (svg) svg.hidden = false;
                    shell?.classList.remove('vector-ready');
                    finish(false);
                }
            }, 9000);

            map.once('load', () => {
                window.clearTimeout(timeout);

                map.addSource('navidpro-branches', {
                    type: 'geojson',
                    data: branchGeoJson
                });

                map.addLayer({
                    id: 'navidpro-branch-halo',
                    type: 'circle',
                    source: 'navidpro-branches',
                    paint: {
                        'circle-radius': [
                            'interpolate',
                            ['linear'],
                            ['zoom'],
                            4, 8,
                            8, 11,
                            13, 15
                        ],
                        'circle-color': '#e8192c',
                        'circle-opacity': 0.22,
                        'circle-stroke-color': '#e8192c',
                        'circle-stroke-width': 2
                    }
                });

                map.addLayer({
                    id: 'navidpro-branch-core',
                    type: 'circle',
                    source: 'navidpro-branches',
                    paint: {
                        'circle-radius': [
                            'interpolate',
                            ['linear'],
                            ['zoom'],
                            4, 4.5,
                            8, 6,
                            13, 8.5
                        ],
                        'circle-color': '#ffffff',
                        'circle-stroke-color': '#e8192c',
                        'circle-stroke-width': 2.5
                    }
                });

                map.addLayer({
                    id: 'navidpro-branch-active',
                    type: 'circle',
                    source: 'navidpro-branches',
                    filter: ['==', ['get', 'id'], '__none__'],
                    paint: {
                        'circle-radius': [
                            'interpolate',
                            ['linear'],
                            ['zoom'],
                            4, 12,
                            8, 17,
                            13, 23
                        ],
                        'circle-color': '#e8192c',
                        'circle-opacity': 0.18,
                        'circle-stroke-color': '#ff5362',
                        'circle-stroke-width': 3
                    }
                });

                branchLayerReady = true;

                const activateFromMapEvent = event => {
                    const feature = event.features?.[0];
                    const branch = branchById(feature?.properties?.id);
                    if (branch) activateVectorBranch(branch);
                };

                ['navidpro-branch-core', 'navidpro-branch-halo'].forEach(layerId => {
                    map.on('click', layerId, activateFromMapEvent);
                    map.on('mouseenter', layerId, () => {
                        map.getCanvas().style.cursor = 'pointer';
                    });
                    map.on('mouseleave', layerId, () => {
                        map.getCanvas().style.cursor = '';
                    });
                });

                if (logoUrl) {
                    const logoImage = new Image();
                    logoImage.decoding = 'async';

                    logoImage.onload = () => {
                        if (map.hasImage('navidpro-branch-logo'))
                            return;

                        map.addImage('navidpro-branch-logo', logoImage);

                        const imageWidth =
                            Number(logoImage.naturalWidth || logoImage.width) || 256;
                        const iconSize =
                            Math.max(0.035, Math.min(0.24, 27 / imageWidth));

                        map.addLayer({
                            id: 'navidpro-branch-logo',
                            type: 'symbol',
                            source: 'navidpro-branches',
                            layout: {
                                'icon-image': 'navidpro-branch-logo',
                                'icon-size': iconSize,
                                'icon-allow-overlap': true,
                                'icon-ignore-placement': true
                            }
                        });

                        map.on(
                            'click',
                            'navidpro-branch-logo',
                            activateFromMapEvent
                        );
                        map.on('mouseenter', 'navidpro-branch-logo', () => {
                            map.getCanvas().style.cursor = 'pointer';
                        });
                        map.on('mouseleave', 'navidpro-branch-logo', () => {
                            map.getCanvas().style.cursor = '';
                        });
                    };

                    logoImage.onerror = () => {
                        console.warn(
                            '[NavidPro Map] Branch logo could not be loaded:',
                            logoUrl
                        );
                    };

                    logoImage.src = logoUrl;
                }

                console.info(
                    '[NavidPro Map] Branch layer ready:',
                    branchGeoJson.features.length,
                    'branches'
                );

                resetVectorView();
                hideLoading();
                finish(true);
            });

            map.once('error', event => {
                if (settled) return;
                if (!event?.error) return;
            });
        });
    };

    const initFallbackMap = () => {
        if (!svg || !world || !provinceLayer || !markerLayer || !geoUrl) {
            showLoading('نقشه آفلاین در دسترس نیست.');
            return;
        }

        canvas && (canvas.hidden = true);
        svg.hidden = false;
        shell?.classList.remove('vector-ready');
        showLoading('در حال آماده‌سازی نقشه محلی…');

        const width = 1000;
        const height = 760;
        const bounds = {
            minLon: 44,
            maxLon: 63.5,
            minLat: 24.5,
            maxLat: 40.5
        };
        const clamp = (value, min, max) =>
            Math.min(max, Math.max(min, value));
        const view = { scale: 1, tx: 0, ty: 0 };
        const markers = new Map();

        let dragging = false;
        let dragPointer = null;
        let dragStart = null;

        const project = (lng, lat) => ({
            x: ((Number(lng) - bounds.minLon) /
                (bounds.maxLon - bounds.minLon)) * width,
            y: ((bounds.maxLat - Number(lat)) /
                (bounds.maxLat - bounds.minLat)) * height
        });

        const pathForRing = ring => {
            if (!Array.isArray(ring) || ring.length === 0) return '';

            return ring
                .map((point, index) => {
                    const p = project(point[0], point[1]);
                    return `${index === 0 ? 'M' : 'L'}${p.x.toFixed(2)},${p.y.toFixed(2)}`;
                })
                .join(' ') + ' Z';
        };

        const pathForGeometry = geometry => {
            if (!geometry) return '';

            if (geometry.type === 'Polygon')
                return geometry.coordinates.map(pathForRing).join(' ');

            if (geometry.type === 'MultiPolygon')
                return geometry.coordinates
                    .flatMap(poly => poly.map(pathForRing))
                    .join(' ');

            return '';
        };

        const updateTransform = () => {
            world.setAttribute(
                'transform',
                `translate(${view.tx.toFixed(2)} ${view.ty.toFixed(2)}) scale(${view.scale.toFixed(4)})`
            );

            markerLayer
                .querySelectorAll('[data-marker-visual]')
                .forEach(node => {
                    node.setAttribute(
                        'transform',
                        `scale(${(1 / view.scale).toFixed(4)})`
                    );
                });
        };

        const setZoom = (
            nextScale,
            focusX = width / 2,
            focusY = height / 2
        ) => {
            const oldScale = view.scale;
            const scale = clamp(nextScale, 1, 7);
            if (scale === oldScale) return;

            const worldX = (focusX - view.tx) / oldScale;
            const worldY = (focusY - view.ty) / oldScale;

            view.scale = scale;
            view.tx = focusX - worldX * scale;
            view.ty = focusY - worldY * scale;
            updateTransform();
        };

        const resetView = () => {
            view.scale = 1;
            view.tx = 0;
            view.ty = 0;
            updateTransform();
        };

        const clientToSvg = (clientX, clientY) => {
            const rect = svg.getBoundingClientRect();
            return {
                x: (clientX - rect.left) *
                    (width / Math.max(1, rect.width)),
                y: (clientY - rect.top) *
                    (height / Math.max(1, rect.height))
            };
        };

        const activateFallbackBranch = branch => {
            if (!branch) return;

            setActiveUi(branch);

            markers.forEach((node, id) => {
                node.classList.toggle(
                    'is-active',
                    String(id) === String(branch.id)
                );
            });

            const coordinates = normalizeBranchCoordinates(branch);
            if (!coordinates) return;

            const p = project(coordinates.lng, coordinates.lat);
            const targetScale = Math.max(view.scale, 3.4);

            view.scale = targetScale;
            view.tx = width / 2 - p.x * targetScale;
            view.ty = height / 2 - p.y * targetScale;
            updateTransform();
        };

        const createMarker = branch => {
            const coordinates = normalizeBranchCoordinates(branch);
            if (!coordinates) return;

            const p = project(coordinates.lng, coordinates.lat);
            const ns = 'http://www.w3.org/2000/svg';
            const group = document.createElementNS(ns, 'g');

            group.setAttribute('class', 'home-map-marker');
            group.setAttribute(
                'transform',
                `translate(${p.x.toFixed(2)} ${p.y.toFixed(2)})`
            );
            group.setAttribute('tabindex', '0');
            group.setAttribute('role', 'button');

            const visual = document.createElementNS(ns, 'g');
            visual.setAttribute('data-marker-visual', '');

            const ring = document.createElementNS(ns, 'circle');
            ring.setAttribute('class', 'home-map-marker-ring');
            ring.setAttribute('r', '24');

            const core = document.createElementNS(ns, 'circle');
            core.setAttribute('class', 'home-map-marker-core');
            core.setAttribute('r', '17');

            visual.append(ring, core);

            if (logoUrl) {
                const image = document.createElementNS(ns, 'image');
                image.setAttribute('href', logoUrl);
                image.setAttribute('x', '-13');
                image.setAttribute('y', '-13');
                image.setAttribute('width', '26');
                image.setAttribute('height', '26');
                visual.appendChild(image);
            }

            const labelWrap = document.createElementNS(ns, 'g');
            const labelWidth = clamp(
                (branch.name || '').length * 7.4 + 28,
                78,
                190
            );

            const labelBg = document.createElementNS(ns, 'rect');
            labelBg.setAttribute('class', 'home-map-marker-label-bg');
            labelBg.setAttribute('x', '28');
            labelBg.setAttribute('y', '-15');
            labelBg.setAttribute('rx', '10');
            labelBg.setAttribute('width', String(labelWidth));
            labelBg.setAttribute('height', '30');

            const label = document.createElementNS(ns, 'text');
            label.setAttribute('class', 'home-map-marker-label');
            label.setAttribute('x', '40');
            label.setAttribute('y', '1');
            label.textContent = branch.name || 'نوید ژلاتو';

            labelWrap.append(labelBg, label);
            visual.appendChild(labelWrap);
            group.appendChild(visual);

            const activate = event => {
                event?.stopPropagation?.();
                activateFallbackBranch(branch);
            };

            group.addEventListener('click', activate);
            group.addEventListener('keydown', event => {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    activate(event);
                }
            });

            markers.set(branch.id, group);
            markerLayer.appendChild(group);
        };

        bindBranchList(activateFallbackBranch);

        root.querySelector('[data-home-map-zoom-in]')?.addEventListener(
            'click',
            () => setZoom(view.scale * 1.35)
        );
        root.querySelector('[data-home-map-zoom-out]')?.addEventListener(
            'click',
            () => setZoom(view.scale / 1.35)
        );
        root.querySelector('[data-home-map-reset]')?.addEventListener(
            'click',
            resetView
        );

        stage.addEventListener(
            'wheel',
            event => {
                event.preventDefault();
                const point = clientToSvg(
                    event.clientX,
                    event.clientY
                );
                setZoom(
                    view.scale * (event.deltaY < 0 ? 1.17 : 0.86),
                    point.x,
                    point.y
                );
            },
            { passive: false }
        );

        stage.addEventListener('pointerdown', event => {
            if (event.button !== 0) return;

            dragging = true;
            dragPointer = event.pointerId;

            const point = clientToSvg(
                event.clientX,
                event.clientY
            );

            dragStart = {
                x: point.x,
                y: point.y,
                tx: view.tx,
                ty: view.ty
            };

            stage.setPointerCapture?.(event.pointerId);
            stage.classList.add('is-dragging');
        });

        stage.addEventListener('pointermove', event => {
            if (
                !dragging ||
                event.pointerId !== dragPointer ||
                !dragStart
            ) return;

            const point = clientToSvg(
                event.clientX,
                event.clientY
            );

            view.tx =
                dragStart.tx + (point.x - dragStart.x);
            view.ty =
                dragStart.ty + (point.y - dragStart.y);

            updateTransform();
        });

        const endDrag = event => {
            if (
                !dragging ||
                (event?.pointerId != null &&
                    event.pointerId !== dragPointer)
            ) return;

            dragging = false;
            dragPointer = null;
            dragStart = null;
            stage.classList.remove('is-dragging');
        };

        stage.addEventListener('pointerup', endDrag);
        stage.addEventListener('pointercancel', endDrag);

        stage.addEventListener('dblclick', event => {
            const point = clientToSvg(
                event.clientX,
                event.clientY
            );
            setZoom(view.scale * 1.45, point.x, point.y);
        });

        fetch(geoUrl, {
            cache: 'force-cache',
            credentials: 'same-origin'
        })
            .then(response => {
                if (!response.ok)
                    throw new Error('Map data unavailable');

                return response.json();
            })
            .then(geo => {
                provinceLayer.replaceChildren();
                markerLayer.replaceChildren();

                (geo?.features || []).forEach(feature => {
                    const d = pathForGeometry(feature.geometry);
                    if (!d) return;

                    const path = document.createElementNS(
                        'http://www.w3.org/2000/svg',
                        'path'
                    );
                    path.setAttribute(
                        'class',
                        'home-map-province'
                    );
                    path.setAttribute('d', d);
                    provinceLayer.appendChild(path);
                });

                branches.forEach(createMarker);
                updateTransform();
                hideLoading();
            })
            .catch(() => {
                showLoading(
                    'نقشه پایه بارگذاری نشد؛ اطلاعات شعب از صفحه شعب قابل مشاهده است.'
                );
            });
    };

    initVectorMap()
        .then(vectorReady => {
            if (!vectorReady)
                initFallbackMap();
        })
        .catch(() => initFallbackMap());
})();