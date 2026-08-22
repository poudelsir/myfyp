// AI features on the Create/Edit Listing form (Areas/Student/Views/Listings/_Form.cshtml):
// "Generate with AI" (title/description/keywords) and "AI Price Recommendation". Both
// read the fields already on the page and fill in suggestions the seller reviews and
// can edit before saving — nothing here submits the form or overwrites a field on its own.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initDescriptionGenerator();
        initPriceRecommendation();
    });

    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    }

    function initDescriptionGenerator() {
        var button = document.getElementById("aiGenerateBtn");
        if (!button) {
            return;
        }

        var statusEl = document.getElementById("aiGenerateStatus");
        var keywordsEl = document.getElementById("aiKeywordsHint");
        var titleInput = document.getElementById("Title");
        var descriptionInput = document.getElementById("Description");
        var categorySelect = document.getElementById("subcategorySelect");
        var subjectSelect = document.getElementById("subjectIdInput");
        var conditionSelect = document.getElementById("Condition");
        var priceInput = document.getElementById("priceInput");
        var donationToggle = document.getElementById("isDonationInput");

        button.addEventListener("click", function () {
            if (!categorySelect.value || !subjectSelect.value) {
                setStatus(statusEl, "Please choose a category and subject first.", true);
                return;
            }

            var body = new URLSearchParams();
            body.append("title", titleInput.value || "");
            body.append("condition", conditionSelect.value);
            body.append("categoryId", categorySelect.value);
            body.append("subjectId", subjectSelect.value);
            body.append("priceAmount", priceInput.value || "0");
            body.append("isDonation", donationToggle.checked ? "true" : "false");
            var token = getToken();
            if (token) {
                body.append("__RequestVerificationToken", token);
            }

            setBusy(button, true);
            setStatus(statusEl, "Generating…", false);
            keywordsEl.classList.add("d-none");

            postForm(button.dataset.generateUrl, body)
                .then(function (suggestion) {
                    titleInput.value = suggestion.title;
                    descriptionInput.value = suggestion.description;
                    setStatus(statusEl, "Suggestion applied — feel free to edit it before saving.", false);

                    if (suggestion.keywords && suggestion.keywords.length > 0) {
                        keywordsEl.textContent = "Suggested keywords: " + suggestion.keywords.join(", ");
                        keywordsEl.classList.remove("d-none");
                    }
                })
                .catch(function (error) {
                    setStatus(statusEl, error.message || "Could not generate a description right now.", true);
                })
                .finally(function () {
                    setBusy(button, false);
                });
        });
    }

    function initPriceRecommendation() {
        var button = document.getElementById("aiPriceBtn");
        if (!button) {
            return;
        }

        var statusEl = document.getElementById("aiPriceStatus");
        var resultEl = document.getElementById("aiPriceResult");
        var suggestedEl = document.getElementById("aiPriceSuggested");
        var rangeEl = document.getElementById("aiPriceRange");
        var confidenceEl = document.getElementById("aiPriceConfidence");
        var explanationEl = document.getElementById("aiPriceExplanation");
        var applyBtn = document.getElementById("aiApplyPriceBtn");
        var titleInput = document.getElementById("Title");
        var descriptionInput = document.getElementById("Description");
        var categorySelect = document.getElementById("subcategorySelect");
        var subjectSelect = document.getElementById("subjectIdInput");
        var conditionSelect = document.getElementById("Condition");
        var priceInput = document.getElementById("priceInput");
        var donationToggle = document.getElementById("isDonationInput");

        var lastSuggestedPrice = null;

        function syncDonationState() {
            button.disabled = donationToggle.checked;
            button.classList.toggle("disabled", donationToggle.checked);
            if (donationToggle.checked) {
                setStatus(statusEl, "Not applicable to donation listings.", false);
                resultEl.classList.add("d-none");
            } else {
                statusEl.textContent = "";
            }
        }

        donationToggle?.addEventListener("change", syncDonationState);
        syncDonationState();

        button.addEventListener("click", function () {
            if (!categorySelect.value || !subjectSelect.value) {
                setStatus(statusEl, "Please choose a category and subject first.", true);
                return;
            }

            if (!titleInput.value.trim() || descriptionInput.value.trim().length < 20) {
                setStatus(statusEl, "Please write a title and at least 20 characters of description first.", true);
                return;
            }

            var body = new URLSearchParams();
            body.append("id", button.dataset.listingId || "0");
            body.append("title", titleInput.value);
            body.append("description", descriptionInput.value);
            body.append("condition", conditionSelect.value);
            body.append("categoryId", categorySelect.value);
            body.append("subjectId", subjectSelect.value);
            body.append("isDonation", donationToggle.checked ? "true" : "false");
            var token = getToken();
            if (token) {
                body.append("__RequestVerificationToken", token);
            }

            setBusy(button, true);
            setStatus(statusEl, "Analyzing marketplace pricing…", false);
            resultEl.classList.add("d-none");

            postForm(button.dataset.priceUrl, body)
                .then(function (recommendation) {
                    lastSuggestedPrice = recommendation.suggestedPrice;
                    suggestedEl.textContent = formatPrice(recommendation.suggestedPrice);
                    rangeEl.textContent = formatPrice(recommendation.suggestedMinPrice) + " – " + formatPrice(recommendation.suggestedMaxPrice);
                    confidenceEl.textContent = recommendation.confidence;
                    explanationEl.textContent = recommendation.explanation;
                    resultEl.classList.remove("d-none");
                    setStatus(statusEl, "", false);
                })
                .catch(function (error) {
                    setStatus(statusEl, error.message || "Could not generate a price recommendation right now.", true);
                })
                .finally(function () {
                    setBusy(button, false);
                });
        });

        applyBtn.addEventListener("click", function () {
            if (lastSuggestedPrice !== null) {
                priceInput.value = lastSuggestedPrice;
            }
        });
    }

    function postForm(url, body) {
        return fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: body.toString(),
            credentials: "same-origin",
        }).then(function (response) {
            if (!response.ok) {
                return response.text().then(function (message) {
                    throw new Error(message || "Request failed.");
                });
            }
            return response.json();
        });
    }

    function formatPrice(amount) {
        return Number(amount).toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    }

    function setBusy(button, isBusy) {
        button.disabled = isBusy;
        button.classList.toggle("disabled", isBusy);
    }

    function setStatus(statusEl, message, isError) {
        statusEl.textContent = message;
        statusEl.classList.toggle("text-danger", isError);
        statusEl.classList.toggle("text-secondary", !isError);
    }
})();
