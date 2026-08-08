// Admin AI Insights dashboard (Areas/Admin/Views/Insights/Index.cshtml). Charts render
// immediately from data the server already embedded (no AI involved). The AI summary
// loads separately via fetch() — if that call fails, the warning banner shows and
// everything else on the page (stat cards, charts) is completely unaffected, matching
// Phase 10.4's "never break the dashboard" requirement.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        renderCharts();
        loadSummary();

        var refreshBtn = document.getElementById("insightsRefreshBtn");
        refreshBtn?.addEventListener("click", loadSummary);
    });

    function renderCharts() {
        if (!window.Chart) {
            return;
        }

        var categories = readJson("insightsCategoryData");
        var universities = readJson("insightsUniversityData");

        renderBarChart("topCategoriesChart", categories, "#0d6efd");
        renderBarChart("topUniversitiesChart", universities, "#198754");
    }

    function renderBarChart(canvasId, items, color) {
        var canvas = document.getElementById(canvasId);
        if (!canvas || !items || items.length === 0) {
            return;
        }

        new window.Chart(canvas, {
            type: "bar",
            data: {
                labels: items.map(function (item) { return item.name; }),
                datasets: [{
                    label: "Active listings",
                    data: items.map(function (item) { return item.count; }),
                    backgroundColor: color,
                    borderRadius: 4,
                }],
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
            },
        });
    }

    function loadSummary() {
        var loadingEl = document.getElementById("insightsLoading");
        var errorEl = document.getElementById("insightsError");
        var contentEl = document.getElementById("insightsContent");
        var summaryEl = document.getElementById("insightsSummary");
        var listEl = document.getElementById("insightsList");
        var refreshBtn = document.getElementById("insightsRefreshBtn");

        loadingEl.classList.remove("d-none");
        errorEl.classList.add("d-none");
        contentEl.classList.add("d-none");
        if (refreshBtn) {
            refreshBtn.disabled = true;
        }

        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        var body = new URLSearchParams();
        if (token) {
            body.append("__RequestVerificationToken", token);
        }

        fetch("/Admin/Insights/Summary", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: body.toString(),
            credentials: "same-origin",
        })
            .then(function (response) {
                if (!response.ok) {
                    return response.text().then(function (message) {
                        throw new Error(message || "Could not generate AI insights right now.");
                    });
                }
                return response.json();
            })
            .then(function (data) {
                summaryEl.textContent = data.summary;
                listEl.innerHTML = "";
                (data.insights || []).forEach(function (insight) {
                    var li = document.createElement("li");
                    li.textContent = insight;
                    listEl.appendChild(li);
                });
                contentEl.classList.remove("d-none");
            })
            .catch(function (error) {
                errorEl.textContent = "AI summary unavailable right now — the statistics above are still accurate. (" +
                    (error.message || "Could not generate AI insights right now.") + ")";
                errorEl.classList.remove("d-none");
            })
            .finally(function () {
                loadingEl.classList.add("d-none");
                if (refreshBtn) {
                    refreshBtn.disabled = false;
                }
            });
    }

    function readJson(elementId) {
        var el = document.getElementById(elementId);
        if (!el) {
            return [];
        }

        try {
            return JSON.parse(el.textContent);
        } catch (e) {
            return [];
        }
    }
})();
