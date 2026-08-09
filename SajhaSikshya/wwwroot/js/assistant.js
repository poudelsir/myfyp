// Marketplace Assistant chat page (Views/Assistant/Index.cshtml). Talks to
// AssistantController via fetch(); the server holds the authoritative conversation
// history in Session, this script only mirrors it in the DOM. Assistant replies are
// markdown text from Gemini — rendered through a small, deliberately minimal
// markdown-to-HTML converter below rather than pulling in a third-party library for
// one feature; raw text is always HTML-escaped first, so nothing in a model response
// can inject markup.
//
// Layout is a messenger-style shell (see assistant.css): #assistantMessages is the
// only scrolling region, the composer stays pinned below it. Auto-scroll is "smart" —
// it only snaps to the newest message when the reader was already at (or near) the
// bottom, so scrolling up to re-read an earlier answer doesn't get yanked back down by
// the next reply.
const NEAR_BOTTOM_THRESHOLD_PX = 80;
const COMPOSER_MAX_HEIGHT_PX = 120;

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var form = document.getElementById("assistantForm");
        if (!form) {
            return;
        }

        var messagesEl = document.getElementById("assistantMessages");
        var input = document.getElementById("assistantQuestionInput");
        var sendBtn = document.getElementById("assistantSendBtn");
        var resetBtn = document.getElementById("assistantResetBtn");
        var emptyStateTemplate = document.getElementById("assistantEmptyStateTemplate");

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

        // Delegated (not bound per-chip) so it keeps working after Reset swaps in a
        // freshly-cloned empty state.
        messagesEl.addEventListener("click", function (event) {
            var chip = event.target.closest(".assistant-suggestion-chip");
            if (chip) {
                sendQuestion(chip.textContent);
            }
        });

        input.addEventListener("input", autoGrowInput);

        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                if (typeof form.requestSubmit === "function") {
                    form.requestSubmit();
                } else {
                    sendQuestion(input.value);
                }
            }
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
                messagesEl.innerHTML = "";
                if (emptyStateTemplate) {
                    messagesEl.appendChild(emptyStateTemplate.content.cloneNode(true));
                }
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

            var emptyState = document.getElementById("assistantEmptyState");
            if (emptyState) {
                emptyState.remove();
            }

            appendBubble("user", question, true);
            input.value = "";
            autoGrowInput();
            setBusy(true);
            showTyping();

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
                    hideTyping();
                    appendBubble("assistant", data.reply, false);
                })
                .catch(function (error) {
                    hideTyping();
                    appendBubble("assistant", "⚠️ " + (error.message || "Could not reach the assistant right now."), false);
                })
                .finally(function () {
                    setBusy(false);
                    input.focus();
                });
        }

        function appendBubble(role, rawText, forceScroll) {
            var shouldStick = forceScroll || isNearBottom();

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

            if (shouldStick) {
                scrollToBottom();
            }
        }

        function showTyping() {
            var shouldStick = isNearBottom();

            var row = document.createElement("div");
            row.className = "assistant-message assistant-message-assistant assistant-message-typing";
            row.id = "assistantTypingBubble";

            var bubble = document.createElement("div");
            bubble.className = "assistant-message-bubble assistant-typing-bubble";
            bubble.setAttribute("aria-label", "Sajha AI is thinking");

            var dots = document.createElement("span");
            dots.className = "assistant-typing-dots";
            dots.innerHTML = "<span></span><span></span><span></span>";
            bubble.appendChild(dots);

            row.appendChild(bubble);
            messagesEl.appendChild(row);

            if (shouldStick) {
                scrollToBottom();
            }
        }

        function hideTyping() {
            document.getElementById("assistantTypingBubble")?.remove();
        }

        function setBusy(isBusy) {
            input.disabled = isBusy;
            sendBtn.disabled = isBusy;
            sendBtn.classList.toggle("is-loading", isBusy);
        }

        function autoGrowInput() {
            input.style.height = "auto";
            input.style.height = Math.min(input.scrollHeight, COMPOSER_MAX_HEIGHT_PX) + "px";
        }

        function isNearBottom() {
            return messagesEl.scrollHeight - messagesEl.scrollTop - messagesEl.clientHeight < NEAR_BOTTOM_THRESHOLD_PX;
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
