// Conversation Mode JavaScript
let conversationHistory = [];

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    const savedLang = localStorage.getItem('selectedLanguage') || 'ko';
    document.getElementById('languageSelect').value = savedLang;
});

function startNewConversation() {
    conversationHistory = [];
    const chatMessages = document.getElementById('chatMessages');
    chatMessages.innerHTML = `
        <div class="message assistant">
            <p>Xin chào! Tôi sẵn sàng trò chuyện với bạn. Hãy bắt đầu nhé! 👋</p>
        </div>
    `;
}

function handleKeyPress(event) {
    if (event.key === 'Enter') {
        sendMessage();
    }
}

async function sendMessage() {
    const input = document.getElementById('messageInput');
    const message = input.value.trim();
    if (!message) return;

    const language = document.getElementById('languageSelect').value;
    const scenario = document.getElementById('scenarioSelect').value;
    const chatMessages = document.getElementById('chatMessages');

    // Add user message
    chatMessages.innerHTML += `
        <div class="message user">
            <p>${escapeHtml(message)}</p>
        </div>
    `;
    input.value = '';
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // Add loading
    const loadingId = Date.now();
    chatMessages.innerHTML += `
        <div class="message assistant" id="loading-${loadingId}">
            <div class="spinner" style="width: 24px; height: 24px;"></div>
        </div>
    `;
    chatMessages.scrollTop = chatMessages.scrollHeight;

    try {
        const response = await fetch(`${window.API_BASE_URL}/api/conversation/chat`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                language,
                scenario,
                message,
                history: conversationHistory.map(h => ({ role: h.role, content: h.content }))
            })
        });

        const data = await response.json();

        // Update history
        conversationHistory.push({ role: 'user', content: message });
        conversationHistory.push({ role: 'assistant', content: data.reply });

        // Remove loading and add response
        document.getElementById(`loading-${loadingId}`).remove();
        chatMessages.innerHTML += `
            <div class="message assistant">
                <p>${formatMessage(data.reply)}</p>
                ${data.translation ? `<p style="font-size: 0.875rem; opacity: 0.7; margin-top: 0.5rem; border-top: 1px solid var(--border-glass); padding-top: 0.5rem;">📝 ${escapeHtml(data.translation)}</p>` : ''}
            </div>
        `;
    } catch (error) {
        document.getElementById(`loading-${loadingId}`).remove();
        chatMessages.innerHTML += `
            <div class="message assistant">
                <p style="color: var(--accent-pink);">❌ Lỗi kết nối. Vui lòng thử lại.</p>
            </div>
        `;
        console.error('Chat error:', error);
    }

    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatMessage(text) {
    return escapeHtml(text).replace(/\n/g, '<br>');
}
