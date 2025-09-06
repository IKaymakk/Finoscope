const customerSelect = document.getElementById('customerSelect');
const startDateInput = document.getElementById('startDate');
const endDateInput = document.getElementById('endDate');
const filterBtn = document.getElementById('filterBtn');
const resultsContainer = document.getElementById('resultsContainer');
const loadingIndicator = document.getElementById('loadingIndicator');
const errorMessage = document.getElementById('errorMessage');
const errorText = document.getElementById('errorText');
const maxDebtSummary = document.getElementById('maxDebtSummary');
const maxDateSummary = document.getElementById('maxDateSummary');
const maxBalanceSummary = document.getElementById('maxBalanceSummary');

let debtChart = null;


// API'den tüm müşterilerin listesini çekiyorz
async function loadCustomers() {
    try {
        const response = await fetch('/api/customers');
        if (!response.ok) {
            throw new Error('Müşteri listesi çekilirken hata oluştu.');
        }
        const customers = await response.json();

        customers.forEach(c => {
            const customerList = document.createElement('option');
            customerList.value = c.id;
            customerList.text = c.unvan;
            customerSelect.add(customerList);
        });
    } catch (e) {
        console.error(e);
        errorMessage.classList.remove('hidden');
        errorText.textContent = 'Müşteriler yüklenemedi. Lütfen API Adresinin çalıştığından emin olun.';
    }
}


// Seçilen müşteri ve tarih aralığına göre borç seyrini API'den çeker ve grafiği günceller.

async function loadDebtTimeline() {
    const customerId = customerSelect.value;
    const start = startDateInput.value;
    const end = endDateInput.value;

    errorMessage.classList.add('hidden');
    resultsContainer.classList.add('hidden');
    maxDebtSummary.classList.add('hidden');

    if (!customerId) {
        errorMessage.classList.remove('hidden');
        errorText.textContent = 'Lütfen bir müşteri seçin.';
        return;
    }

    resultsContainer.classList.remove('hidden');
    loadingIndicator.classList.remove('hidden');
    errorMessage.classList.add('hidden');
    maxDebtSummary.classList.add('hidden');

    try {
        let apiUrl = `/api/customers/${customerId}/balances/timeline`;
        const params = new URLSearchParams();

        if (start) {
            params.append('start', new Date(start).toISOString());
        }
        if (end) {
            params.append('end', new Date(end).toISOString());
        }

        if (params.toString()) {
            apiUrl += `?${params.toString()}`;
        }

        const response = await fetch(apiUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        loadingIndicator.classList.add('hidden');

        if (data && data.points && data.points.length > 0) {
            renderChart(data);
        } else {
            errorMessage.classList.remove('hidden');
            errorText.textContent = 'Seçilen kriterlere göre borç verisi bulunamadı.';
            if (debtChart) debtChart.destroy();
        }
    } catch (e) {
        console.error('Hata detayı:', e);
        loadingIndicator.classList.add('hidden');
        errorMessage.classList.remove('hidden');
        errorText.textContent = `Veri çekilirken bir hata oluştu: ${e.message}`;
        if (debtChart) debtChart.destroy();
    }
}

/**
 * Chart.js grafiğini çizer veya günceller.
 */
function renderChart(data) {
    const ctx = document.getElementById('debtChart').getContext('2d');
    if (debtChart) {
        debtChart.destroy();
    }

    const labels = data.points.map(item => new Date(item.date).toLocaleDateString('tr-TR'));
    const dataPoints = data.points.map(item => item.endOfDayBalance);

    let maxDebtIndex = -1;
    let maxDebt = null;
    let maxBalance = -1;

    // Borç verileri arasında en yüksek borcu bul
    data.points.forEach((point, index) => {
        if (point.endOfDayBalance > maxBalance) {
            maxBalance = point.endOfDayBalance;
            maxDebt = point;
            maxDebtIndex = index;
        }
    });

    const pointBackgroundColors = dataPoints.map((val, index) => index === maxDebtIndex ? 'red' : '#2563eb');
    const pointRadii = dataPoints.map((val, index) => index === maxDebtIndex ? 7 : 3);

    // Grafik arka plan renk
    const chartBackgroundGradient = ctx.createLinearGradient(0, 0, 0, 400);
    chartBackgroundGradient.addColorStop(0, 'rgba(99, 102, 241, 0.2)'); 
    chartBackgroundGradient.addColorStop(1, 'rgba(119, 52, 146, 0.05)'); 

    debtChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Günlük Borç',
                data: dataPoints,
                borderColor: '#6366f1',
                backgroundColor: chartBackgroundGradient,
                tension: 0.4,
                fill: true,
                pointBackgroundColor: pointBackgroundColors,
                pointRadius: pointRadii,
                pointHoverRadius: 10,
                pointHoverBorderColor: 'red',
                pointHoverBorderWidth: 2,
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
                duration: 1500,
                easing: 'easeInOutQuad'
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `Borç: ${context.parsed.y.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ₺`;
                        }
                    },
                    backgroundColor: 'rgba(0, 0, 0, 0.7)',
                    titleFont: { weight: 'bold' }
                }
            },
            scales: {
                x: {
                    title: { display: true, text: 'Tarih', color: '#4b5563' },
                    ticks: { color: '#6b7280' },
                    grid: { color: 'rgba(209, 213, 219, 0.5)' }
                },
                y: {
                    title: { display: true, text: 'Borç (₺)', color: '#4b5563' },
                    beginAtZero: false,
                    ticks: {
                        color: '#6b7280',
                        callback: function (value) {
                            return value.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
                        }
                    },
                    grid: { color: 'rgba(209, 213, 219, 0.5)' }
                }
            }
        }
    });

    // Sadece veri varsa özet bilgiyi göster
    if (maxDebt) {
        maxDateSummary.textContent = new Date(maxDebt.date).toLocaleDateString();
        maxBalanceSummary.textContent = `${maxDebt.endOfDayBalance.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
        maxDebtSummary.classList.remove('hidden');
    } else {
        maxDebtSummary.classList.add('hidden');
    }
}

filterBtn.addEventListener('click', loadDebtTimeline);

document.addEventListener('DOMContentLoaded', loadCustomers);