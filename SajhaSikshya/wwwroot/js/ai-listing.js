// "Generate with AI" on the Create/Edit Listing form (Areas/Student/Views/Listings/_Form.cshtml).
// Reads the fields already on the page, posts them to ListingsController.GenerateDescription,
// and fills Title/Description in place — the seller reviews and can edit everything
// before saving, nothing here submits the form on its own.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var button = document.getElementById("aiGenerateBtn");
        if (!button) {
            return;
        }

        var statusEl = document.getElementById("aiGenerateStatus");
        var keywordsEl = document.getElementById("aiKeywordsHint");
        var titleInput = document.getElementById("Title");
        var descriptionInput = document.getElementById("Description");
        var categorySelect = document.getElementById("CategoryId");
        var subjectSelect = document.getElementById("SubjectId");
        var conditionSelect = document.getElementById("Condition");
        var priceInput = document.getElementById("priceInput");
        var donationToggle = document.getElementById("isDonationInput");

        button.addEventListener("click", function () {
            if (!categorySelect.value || !subjectSelect.value) {
                setStatus("Please choose a category and subject first.", true);
                return;
            }

            var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            var body = new URLSearchParams();
            body.append("title", titleInput.value || "");
            body.append("condition", conditionSelect.value);
            body.append("categoryId", categorySelect.value);
            body.append("subjectId", subjectSelect.value);
            body.append("priceAmount", priceInput.value || "0");
            body.append("isDonation", donationToggle.checked ? "true" : "false");
            if (token) {
                body.append("__RequestVerificationToken", token);
            }

            setBusy(true);
            setStatus("Generating…", false);
            keywordsEl.classList.add("d-none");

            fetch(button.dataset.generateUrl, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: body.toString(),
                credentials: "same-origin",
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.text().then(function (message) {
                            throw new Error(message || "Could not generate a description right now.");
                        });
                    }
                    return response.json();
                })
                .then(function (suggestion) {
                    titleInput.value = suggestion.title;
                    descriptionInput.value = suggestion.description;
                    setStatus("Suggestion applied — feel free to edit it before saving.", false);

                    if (suggestion.keywords && suggestion.keywords.length > 0) {
                        keywordsEl.textContent = "Suggested keywords: " + suggestion.keywords.join(", ");
                        keywordsEl.classList.remove("d-none");
                    }
                })
                .catch(function (error) {
                    setStatus(error.message || "Could not generate a description right now.", true);
                })
                .finally(function () {
                    setBusy(false);
                });
        });

        function setBusy(isBusy) {
            button.disabled = isBusy;
            button.classList.toggle("disabled", isBusy);
        }

        function setStatus(message, isError) {
            statusEl.textContent = message;
            statusEl.classList.toggle("text-danger", isError);
            statusEl.classList.toggle("text-secondary", !isError);
        }
    });
})();
