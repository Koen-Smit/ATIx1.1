# Year 1, Semester 1: ATIx ICT-B1.1 Smart Meter Data Processing (2024-25)
- (04-09-2024 / 01/11/2024)

**Built with:** Blazor (.NET C#, HTML, CSS, Bootstrap, JavaScript)

## Project Overview
This project was developed as part of the first-year, first-period coursework for ATIx ICT-B1.1. The primary goal was to process and visualize data collected from smart meters connected to residential electricity and gas systems. 

### Functionality
- **Data Visualization:** Tables and charts to analyze electricity and gas usage.
- **Dynamic Tariff Calculations:** Algorithms to compute energy costs based on real-time tariffs, including surcharges for high power usage.
- **Customizable Analysis:** Adjustable time periods for data aggregation and analysis.

### Context
This project was graded a 10 (out of 10). However, the application can no longer be used because the database it relies on has been decommissioned and is no longer connected to any live smart meters(as far as I know).

---

## Prerequisites
This project relies on an InfluxDB database to store and query smart meter data. Sensitive credentials like database URLs, tokens, and organization details must be configured on your local machine using `.NET user-secrets`.

### Setting Up InfluxDB

1. **Open a Command Line Interface (CLI):**
   - You can use Command Prompt (CMD), PowerShell, or any Bash-compatible terminal.

2. **Navigate to the Project Directory:**
   ```bash
   cd path_to_your_project/SmartEnergy/SmartEnergy.Client
   ```

3. **Initialize User-Secrets:**
   ```bash
   dotnet user-secrets init
   ```

4. **Set InfluxDB Credentials:**
   Replace placeholders with actual values:
   ```bash
   dotnet user-secrets set "InfluxDb:Url" "your_influxdb_url"
   dotnet user-secrets set "InfluxDb:Token" "your_influxdb_token"
   dotnet user-secrets set "InfluxDb:Org" "your_influxdb_organization"
   ```

---

## Application Structure
The project is split into two main components:

### 1. **SmartEnergy.Client**
   - A Blazor-based web application responsible for front-end functionality.
   - Built with **Bootstrap** for responsive design. [Bootstrap Documentation](https://getbootstrap.com/docs/5.1/getting-started/introduction/)

   **Features:**
   - Interactive dashboards and controls for customizing time periods.
   - Dynamic charts to visualize usage, costs, and surcharges.

### 2. **SmartEnergy.Library**
    (MOSTLY MADE BY SCHOOL!)
   - Contains backend logic for querying the database and processing data.
   - Handles API calls to retrieve smart meter measurements.

---

## Development Environment

### Run Profiles
- **Debug Mode:** Enables breakpoints for backend debugging but requires restarting after code changes.
- **Hot Reload:** Automatically refreshes the browser after code changes but does not support breakpoints.

---

## Core Algorithm
The application calculates energy usage and costs over a customizable time period. It dynamically applies surcharges based on power usage thresholds. Key calculations include:

- **Total Usage (kWh):** Aggregated over the selected time period.
- **Base Cost:** Calculated using dynamic energy tariffs.
- **Surcharge:** Applied progressively based on the following thresholds:
  
  | Power Usage (Watt) | Surcharge (%) |
  |--------------------|---------------|
  | 0 - 1000           | 0%            |
  | 1001 - 2000        | 25%           |
  | 2001 - 3000        | 38%           |
  | 3001 - 4000        | 44%           |
  | > 4000             | 47%           |

---

## Usage Example
The following code snippet demonstrates the main logic:

```csharp
private List<double?> CalculatePrice(List<Measurement> measurements, string aggregationWindow)
{
    if (measurements == null || measurements.Count == 0)
    {
        return new List<double?> { 0, 0, 0, 0 };
    }

    double totalUsage = 0;
    double totalPriceNoAddition = 0;
    double totalPrice = 0;
    double totalSurchargePercentage = 0;
    int surchargeCount = 0;
    object lockObject = new object();

    double timeFactor = aggregationWindow switch
    {
        "20s" => 20.0 / 3600.0,
        "5m" => 5.0 / 60.0,
        "1h" => 1.0,
        _ => throw new ArgumentException("Invalid aggregation window")
    };

    Parallel.ForEach(measurements, measurement =>
    {
        if (measurement.Value == null || measurement.EnergyPrice == null || measurement.Value <= 0 || measurement.EnergyPrice <= 0)
        {
            return;
        }

        double kiloWattValue = measurement.Value.Value / 1000;
        double surchargePercentage = measurement.Value > 4000 ? 0.47 :
                                      measurement.Value > 3000 ? 0.44 :
                                      measurement.Value > 2000 ? 0.38 :
                                      measurement.Value > 1000 ? 0.25 : 0;

        double adjustedEnergyPrice = measurement.EnergyPrice.Value * (1 + surchargePercentage);
        double priceChangeWithSurcharge = adjustedEnergyPrice * kiloWattValue * timeFactor;

        lock (lockObject)
        {
            totalUsage += kiloWattValue;
            totalPriceNoAddition += measurement.EnergyPrice.Value * kiloWattValue * timeFactor;
            totalPrice += priceChangeWithSurcharge;
        }
    });

    double averageSurchargePercentage = surchargeCount > 0 ? totalSurchargePercentage / surchargeCount * 100 : 0;
    return new List<double?> { totalPrice, totalPriceNoAddition, totalUsage, averageSurchargePercentage };
}
```

---

## Limitations
- **Database Dependency:** The application cannot function without a live connection to the InfluxDB database.
- **Scalability:** Designed as a prototype; additional optimizations would be required but are not possible because project ended.

---