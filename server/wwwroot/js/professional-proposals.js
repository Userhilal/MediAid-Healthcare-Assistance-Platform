// Professional Proposals Page JavaScript
// Handles map initialization, detail pane, and interactions

let detailPaneOpen = false;
const maps = {};

// Initialize mini maps for all request cards
document.addEventListener('DOMContentLoaded', function() {
    initializeMiniMaps();
    setupCardInteractions();
});

// Initialize Leaflet maps for each request card
function initializeMiniMaps() {
    const mapContainers = document.querySelectorAll('.mini-map-container');
    
    mapContainers.forEach(container => {
        const requestId = container.id.replace('map-', '');
        const lat = parseFloat(container.dataset.lat);
        const lon = parseFloat(container.dataset.lon);
        
        if (isNaN(lat) || isNaN(lon)) return;
        
        // Initialize map
        const map = L.map(container.id, {
            zoomControl: false,
            attributionControl: false,
            dragging: false,
            touchZoom: false,
            doubleClickZoom: false,
            scrollWheelZoom: false,
            boxZoom: false,
            keyboard: false
        }).setView([lat, lon], 13);
        
        // Add tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19
        }).addTo(map);
        
        // Add marker
        L.marker([lat, lon], {
            icon: L.icon({
                iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-red.png',
                iconSize: [20, 32],
                iconAnchor: [10, 32]
            })
        }).addTo(map);
        
        maps[requestId] = map;
    });
}

// Setup card click interactions
function setupCardInteractions() {
    const cards = document.querySelectorAll('.request-card-pro');
    
    cards.forEach(card => {
        card.addEventListener('click', function(e) {
            // Don't open if clicking the propose button
            if (e.target.closest('.btn-propose-pro')) {
                return;
            }
            
            const requestId = card.dataset.requestId;
            openRequestDetails(requestId);
        });
    });
}

// Open request details in right pane
async function openRequestDetails(requestId) {
    // Show loading state
    const detailPaneBody = document.getElementById('detailPaneBody');
    detailPaneBody.innerHTML = `
        <div class="detail-placeholder">
            <div class="loading-spinner"></div>
            <p>Chargement des détails...</p>
        </div>
    `;
    
    // Open pane
    const detailPane = document.getElementById('detailPane');
    detailPane.classList.add('open');
    detailPaneOpen = true;
    
    // Fetch request details
    try {
        const response = await fetch(`/Proposal/DetailsPartial?id=${requestId}`);
        const html = await response.text();
        detailPaneBody.innerHTML = html;
    } catch (error) {
        console.error('Error loading details:', error);
        detailPaneBody.innerHTML = `
            <div class="detail-placeholder">
                <i class="bi bi-exclamation-circle"></i>
                <p>Erreur lors du chargement des détails</p>
            </div>
        `;
    }
}

// Close detail pane
function closeDetailPane() {
    const detailPane = document.getElementById('detailPane');
    detailPane.classList.remove('open');
    detailPaneOpen = false;
}

// Propose help
function proposeHelp(requestId) {
    const button = event.target.closest('.btn-propose-pro');
    
    if (!button) return;
    
    // Add loading state
    button.classList.add('loading');
    button.querySelector('span:not(.btn-glow)').textContent = 'Envoi...';
    
    // Redirect to proposal creation
    window.location.href = `/Proposal/Create?requestId=${requestId}`;
}

// Close pane when clicking outside (on mobile)
document.addEventListener('click', function(e) {
    if (detailPaneOpen && !e.target.closest('.command-sidebar-right') && !e.target.closest('.request-card-pro')) {
        closeDetailPane();
    }
});

// Handle window resize for maps
window.addEventListener('resize', function() {
    Object.values(maps).forEach(map => {
        setTimeout(() => {
            map.invalidateSize();
        }, 100);
    });
});


