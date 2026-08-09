"use strict";

// Free/donation listings don't need a price — lock it at 0 client-side for a
// cleaner UX. Uses readonly (not disabled) so the field still posts; the server
// still authoritatively re-derives Price from IsDonation regardless either way
// (see ListingService), this is just presentation.
(function () {
    var donationToggle = document.getElementById("isDonationInput");
    var priceInput = document.getElementById("priceInput");
    if (!donationToggle || !priceInput) {
        return;
    }

    function syncPriceState() {
        priceInput.readOnly = donationToggle.checked;
        if (donationToggle.checked) {
            priceInput.value = "0";
        }
    }

    donationToggle.addEventListener("change", syncPriceState);
    syncPriceState();
})();

// Department is a client-side narrowing aid only — the Subcategory select is what
// actually gets submitted as CategoryId. All subcategories are rendered server-side
// up front (the whole taxonomy is small), so filtering here is just show/hide by
// each <option>'s data-parent-id, no AJAX round trip needed.
(function () {
    var departmentSelect = document.getElementById("departmentSelect");
    var subcategorySelect = document.getElementById("subcategorySelect");
    if (!departmentSelect || !subcategorySelect) {
        return;
    }

    var allOptions = Array.from(subcategorySelect.querySelectorAll("option[data-parent-id]"));
    var placeholder = subcategorySelect.querySelector("option[value='']");

    function applyFilter(preserveSelection) {
        var departmentId = departmentSelect.value;
        var previouslySelected = subcategorySelect.value;

        subcategorySelect.innerHTML = "";
        subcategorySelect.appendChild(placeholder);

        if (!departmentId) {
            placeholder.textContent = "— Select a department first —";
            return;
        }

        placeholder.textContent = "— Select a subcategory —";
        var matching = allOptions.filter(function (opt) { return opt.dataset.parentId === departmentId; });
        matching.forEach(function (opt) { subcategorySelect.appendChild(opt); });

        if (preserveSelection && matching.some(function (opt) { return opt.value === previouslySelected; })) {
            subcategorySelect.value = previouslySelected;
        }
    }

    departmentSelect.addEventListener("change", function () { applyFilter(false); });
    applyFilter(true);
})();
