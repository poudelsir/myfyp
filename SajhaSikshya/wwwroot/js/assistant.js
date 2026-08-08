// Marketplace Assistant chat page (Views/Assistant/Index.cshtml). Talks to
// AssistantController via fetch(); the server holds the authoritative conversation
// history in Session, this script only mirrors it in the DOM. Assistant replies are
// markdown text from Gemini — rendered through a small, deliberately minimal
// markdown-to-HTML converter below rather than pulling in a third-party library for
// one feature; raw text is always HTML-escaped first, so nothing in a model response
// can inject markup.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var form = document.getElementById("assistantForm");
        if (!form) {
            return;
        }

        var messagesEl = document.getElementById("assistantMessages");
        var typingEl = document.getElementById("assistantTyping");
        var suggestionsEl = document.getElementById("assistantSuggestions");
        var input = document.getElementById("assistantQuestionInput");
        var sendBtn = document.getElementById("assistantSendBtn");
        var resetBtn = document.getElementById("assistantResetBtn");

        // Render markdown for any history bubbles the server already sent down.
        messagesEl.querySelectorAll(".assistant-message-assistant .assistant-message-bubble").forEach(function (bubble) {
            bubble.innerHTML = renderMarkdown(bubble.dataset.rawText || "");
        });
        messagesEl.querySelectorAll(".assistant-message-user .assistant-message-bubble").forEach(function (bubble) {
            bubble.textContent = bubble.dataset.rawText || "";
        });
        scrollToBottom();

        form.addEventListener("submit", function (event) {
            event.preventDefault();
            sendQuestion(input.value);
        });

        suggestionsEl.querySelectorAll(".assistant-suggestion-chip").forEach(function (chip) {
            chip.addEventListener("click", function () {
                sendQuestion(chip.textContent);
            });
        });

        resetBtn.addEventListener("click", function () {
            var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            var body = new URLSearchParams();
            if (token) {
                body.append("__RequestVerificationToken", token);
            }

            fetch("/assistant/reset", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: body.toString(),
                credentials: "same-origin",
            }).then(function () {
                messagesEl.innerHTML =
                    '<div class="assistant-empty-state"><i data-lucide="message-circle-question"></i>' +
                    "<p class=\"mb-0\">Ask me anything about buying, selling, or using SajhaSikshya.</p></div>";
                suggestionsEl.classList.remove("d-none");
                if (window.lucide) {
                    window.lucide.createIcons();
                }
            });
        });

        function sendQuestion(text) {
            var question = (text || "").trim();
            if (!question) {
                return;
            }

            var emptyState = messagesEl.querySelector(".assistant-empty-state");
            if (emptyState) {
                emptyState.remove();
            }

            appendBubble("user", question);
            input.value = "";
            suggestionsEl.classList.add("d-none");
            setBusy(true);

            var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            var body = new URLSearchParams();
            body.append("question", question);
            if (token) {
                body.append("__RequestVerificationToken", token);
            }

            fetch("/assistant/ask", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: body.toString(),
                credentials: "same-origin",
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.text().then(function (message) {
                            throw new Error(message || "Could not reach the assistant right now.");
                        });
                    }
                    return response.json();
                })
                .then(function (data) {
                    appendBubble("assistant", data.reply);
                })
                .catch(function (error) {
                    appendBubble("assistant", "⚠️ " + (error.message || "Could not reach the assistant right now."));
                })
                .finally(function () {
                    setBusy(false);
                });
        }

        function appendBubble(role, rawText) {
            var wrapper = document.createElement("div");
            wrapper.className = "assistant-message assistant-message-" + role;

            var bubble = document.createElement("div");
            bubble.className = "assistant-message-bubble";
            if (role === "assistant") {
                bubble.innerHTML = renderMarkdown(rawText);
            } else {
                bubble.textContent = rawText;
            }

            wrapper.appendChild(bubble);
            messagesEl.appendChild(wrapper);
            scrollToBottom();
        }

        function setBusy(isBusy) {
            input.disabled = isBusy;
            sendBtn.disabled = isBusy;
            typingEl.classList.toggle("d-none", !isBusy);
            if (isBusy) {
                scrollToBottom();
            }
        }

        function scrollToBottom() {
            messagesEl.scrollTop = messagesEl.scrollHeight;
        }
    });

    function escapeHtml(text) {
        var div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }

    function renderInline(text) {
        return text
            .replace(/`([^`]+)`/g, "<code>$1</code>")
            .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>")
            .replace(/(^|[^*])\*([^*]+)\*(?!\*)/g, "$1<em>$2</em>");
    }

    /// Deliberately minimal: headings, bold/italic/code, bullet and numbered lists,
    /// paragraphs. Enough for a chat assistant's typical reply without a full CommonMark
    /// implementation.
    function renderMarkdown(rawText) {
        var escaped = escapeHtml(rawText || "");
        var lines = escaped.split(/\r?\n/);
        var html = "";
        var listItems = [];
        var listTag = null;

        function flushList() {
            if (listTag) {
                html += "<" + listTag + ">" + listItems.join("") + "</" + listTag + ">";
                listItems = [];
                listTag = null;
            }
        }

        lines.forEach(function (line) {
            var heading = line.match(/^(#{1,3})\s+(.*)/);
            var bullet = line.match(/^[-*]\s+(.*)/);
            var numbered = line.match(/^\d+\.\s+(.*)/);

            if (heading) {
                flushList();
                var level = heading[1].length === 1 ? "h5" : "h6";
                html += "<" + level + ">" + renderInline(heading[2]) + "</" + level + ">";
            } else if (bullet) {
                if (listTag !== "ul") {
                    flushList();
                    listTag = "ul";
                }
                listItems.push("<li>" + renderInline(bullet[1]) + "</li>");
            } else if (numbered) {
                if (listTag !== "ol") {
                    flushList();
                    listTag = "ol";
                }
                listItems.push("<li>" + renderInline(numbered[1]) + "</li>");
            } else if (line.trim() === "") {
                flushList();
            } else {
                flushList();
                html += "<p>" + renderInline(line) + "</p>";
            }
        });

        flushList();
        return html || "<p></p>";
    }
})();
