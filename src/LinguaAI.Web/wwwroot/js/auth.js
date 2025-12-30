// Time-Based Authentication Helper
// Generates HMAC-SHA256 auth header with 60-second validity

class TimeBasedAuth {
    constructor(userId, apiKey) {
        this.userId = userId;
        this.apiKey = apiKey;
        this.WINDOW_TICKS = 600000000n; // 60 seconds in .NET ticks
        this.TICKS_EPOCH = 621355968000000000n; // .NET epoch (0001-01-01) to Unix epoch (1970-01-01)
    }

    // Get current UTC ticks (compatible with .NET DateTime.UtcNow.Ticks)
    getUtcTicks() {
        const unixMs = BigInt(Date.now());
        const unixTicks = unixMs * 10000n; // Convert ms to ticks (1 tick = 100 nanoseconds)
        return this.TICKS_EPOCH + unixTicks;
    }

    // Compute SHA256 hash
    async sha256(message) {
        const msgBuffer = new TextEncoder().encode(message);
        const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }

    // Generate time-based password
    async generatePassword(utcTicks) {
        const window = utcTicks / this.WINDOW_TICKS;
        const input = `${this.apiKey}${window}`;
        return await this.sha256(input);
    }

    // Generate full hash
    async generateHash(password) {
        const input = `${this.userId}:${password}`;
        return await this.sha256(input);
    }

    // Generate Authorization header
    async getAuthHeader() {
        const ticks = this.getUtcTicks();
        const password = await this.generatePassword(ticks);
        const hash = await this.generateHash(password);
        return `HMAC-SHA256 ${this.userId}:${hash}`;
    }
}

// Export for use in other files
window.TimeBasedAuth = TimeBasedAuth;
