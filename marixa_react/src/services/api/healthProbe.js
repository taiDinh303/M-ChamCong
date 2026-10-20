// Liveness is anonymous and does not query business data or validate a session.
// API_BASE_URL ends in /api; health endpoints live at the server root.
export const healthProbeUrl = (apiBaseUrl) => `${apiBaseUrl.replace(/\/+$/, "").replace(/\/api$/i, "")}/health/live`;

export function createHealthProbe(client, apiBaseUrl) {
    const url = healthProbeUrl(apiBaseUrl);
    return async ({ timeout = 10_000 } = {}) => {
        const response = await client.get(url, { timeout });
        if (response.status !== 200 || response.data?.data?.status !== "live") {
            const error = new Error("Máy chủ chưa sẵn sàng.");
            error.response = { status: 503 };
            throw error;
        }
    };
}
