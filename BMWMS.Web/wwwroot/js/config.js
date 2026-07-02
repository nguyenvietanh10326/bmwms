const API_CONFIG = {
    baseUrl: 'https://localhost:7194',
    endpoints: {
        categories: '/api/categories'
    }
};

function getApiUrl(endpoint) {
    return API_CONFIG.baseUrl + API_CONFIG.endpoints[endpoint];
}