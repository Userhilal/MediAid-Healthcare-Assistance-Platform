document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(window.location.search);
    const shouldFocusLocation =
        params.get("focus") === "location" ||
        window.location.hash === "#location-settings";

    if (!shouldFocusLocation) {
        return;
    }

    const controls = Array.from(document.querySelectorAll("input, select, textarea"));

    function normalize(value) {
        return (value || "").toString().toLowerCase();
    }

    function findControl(tokens) {
        return controls.find(function (control) {
            const id = normalize(control.id);
            const name = normalize(control.name);
            const placeholder = normalize(control.getAttribute("placeholder"));
            const parentText = normalize(
                control.closest(".form-group, .mb-3, .form-floating, .col-md-6, .col-md-12, .row, section, form")?.innerText
            );

            return tokens.some(function (token) {
                return id.includes(token) ||
                    name.includes(token) ||
                    placeholder.includes(token) ||
                    parentText.includes(token);
            });
        });
    }

    const latitudeInput = findControl(["latitude", "lat"]);
    const longitudeInput = findControl(["longitude", "lng", "lon"]);
    const addressInput = findControl(["adresse", "address", "localisation", "location", "ville", "city"]);

    const targetControl = latitudeInput || longitudeInput || addressInput;
    const targetBlock =
        targetControl?.closest(".card, .profile-card, .form-section, .row, form, .container") ||
        document.querySelector("form") ||
        document.querySelector("main") ||
        document.body;

    if (!targetBlock) {
        return;
    }

    targetBlock.id = "location-settings";
    targetBlock.classList.add("location-focus-target");

    if (!document.querySelector(".location-focus-panel")) {
        const panel = document.createElement("div");
        panel.className = "location-focus-panel";
        panel.innerHTML = `
            <div>
                <strong>Configurer votre localisation</strong>
                <p>
                    Ajoutez votre adresse ou utilisez votre position actuelle pour afficher uniquement les demandes proches de vous.
                </p>
            </div>
            <button type="button" class="location-detect-btn">
                Utiliser ma position actuelle
            </button>
        `;

        targetBlock.parentNode.insertBefore(panel, targetBlock);

        const button = panel.querySelector(".location-detect-btn");

        button.addEventListener("click", function () {
            if (!navigator.geolocation) {
                alert("La géolocalisation n'est pas disponible sur ce navigateur.");
                return;
            }

            button.disabled = true;
            button.textContent = "Recherche de votre position...";

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    if (latitudeInput) {
                        latitudeInput.value = position.coords.latitude.toFixed(6);
                        latitudeInput.dispatchEvent(new Event("input", { bubbles: true }));
                        latitudeInput.dispatchEvent(new Event("change", { bubbles: true }));
                    }

                    if (longitudeInput) {
                        longitudeInput.value = position.coords.longitude.toFixed(6);
                        longitudeInput.dispatchEvent(new Event("input", { bubbles: true }));
                        longitudeInput.dispatchEvent(new Event("change", { bubbles: true }));
                    }

                    button.textContent = "Position ajoutée";
                    button.disabled = false;
                },
                function () {
                    button.textContent = "Réessayer";
                    button.disabled = false;
                    alert("Impossible de récupérer votre position. Vous pouvez la saisir manuellement.");
                },
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 0
                }
            );
        });
    }

    setTimeout(function () {
        document.querySelector(".location-focus-panel")?.scrollIntoView({
            behavior: "smooth",
            block: "center"
        });
    }, 300);
});
