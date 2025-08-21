# Uplink Purchase Analytics - Grafana Queries

This document provides example Grafana queries for visualizing uplink purchase data exported by the `UplinkPurchaseMetricsSystem`.

## Metrics Available

### 1. uplink_purchases_total
Counter tracking the total number of items purchased from uplinks.
- Labels: `uplink_type`, `item`

### 2. uplink_currency_spent_total  
Gauge tracking the total currency spent on uplink purchases.
- Labels: `uplink_type`, `currency`

## Example Grafana Queries

### Total Purchases by Uplink Type (Pie Chart)
```promql
sum by (uplink_type) (uplink_purchases_total)
```

### Purchase Rate by Uplink Type (Time Series)
```promql
rate(uplink_purchases_total[5m])
```

### Top 10 Most Purchased Items (Bar Chart)
```promql
topk(10, sum by (item, uplink_type) (uplink_purchases_total))
```

### Currency Spent by Uplink Type (Stacked Bar Chart)
```promql
sum by (uplink_type, currency) (uplink_currency_spent_total)
```

### Sponsor Uplink Activity (Time Series)
```promql
sum by (item) (rate(uplink_purchases_total{uplink_type="sponsor"}[5m]))
```

### ERD (Emergency Response Division) Purchases (Time Series)
```promql
rate(uplink_purchases_total{uplink_type="erd"}[5m])
```

### Nuclear Operatives Equipment Purchases (Table)
```promql
sum by (item) (uplink_purchases_total{uplink_type="nuclear"})
```

### Traitor Activity Comparison (Time Series)
```promql
sum by (uplink_type) (rate(uplink_purchases_total{uplink_type=~"traitor|nuclear|cult"}[5m]))
```

### Telecrystal Spending Rate (Time Series)
```promql
rate(uplink_currency_spent_total{currency="Telecrystal"}[5m])
```

### Most Active Uplink Type (Single Stat)
```promql
sort_desc(sum by (uplink_type) (increase(uplink_purchases_total[1h])))[0]
```

### Total Revenue by Currency Type (Bar Chart)
```promql
sum by (currency) (uplink_currency_spent_total)
```

### Hourly Purchase Volume Heatmap
```promql
sum by (uplink_type) (increase(uplink_purchases_total[1h]))
```

## Dashboard Panel Examples

### 1. Overview Panel
- **Type**: Single Stat
- **Query**: `sum(uplink_purchases_total)`
- **Title**: "Total Uplink Purchases"

### 2. Uplink Activity Panel
- **Type**: Time Series
- **Query**: `sum by (uplink_type) (rate(uplink_purchases_total[5m]))`
- **Title**: "Purchase Activity by Uplink Type"
- **Legend**: "{{uplink_type}}"

### 3. Economic Activity Panel
- **Type**: Time Series  
- **Query**: `rate(uplink_currency_spent_total[5m])`
- **Title**: "Currency Spending Rate"
- **Legend**: "{{uplink_type}} - {{currency}}"

### 4. Popular Items Panel
- **Type**: Table
- **Query**: `sort_desc(sum by (item, uplink_type) (uplink_purchases_total))`
- **Title**: "Most Popular Items"
- **Columns**: Item, Uplink Type, Total Purchases

### 5. Security Intelligence Panel
- **Type**: Time Series
- **Query**: `sum by (uplink_type) (rate(uplink_purchases_total{uplink_type=~"traitor|nuclear|cult"}[10m]))`
- **Title**: "Antagonist Activity"
- **Legend**: "{{uplink_type}}"

## Alert Examples

### High Antagonist Activity Alert
```promql
sum by (uplink_type) (rate(uplink_purchases_total{uplink_type=~"traitor|nuclear|cult"}[5m])) > 0.1
```

### Unusual Sponsor Activity Alert
```promql
sum(rate(uplink_purchases_total{uplink_type="sponsor"}[5m])) > 0.05
```

### ERD Equipment Depletion Alert
```promql
sum(rate(uplink_currency_spent_total{uplink_type="erd"}[5m])) > 10
```

## Notes

- All queries use the default Prometheus retention and scrape intervals
- Adjust time ranges (`[5m]`, `[1h]`, etc.) based on your monitoring needs
- Use `increase()` for cumulative counts over time periods
- Use `rate()` for per-second rates of change
- Filter by specific uplink types using regex: `{uplink_type=~"sponsor|erd"}`