using SmartEnergy.Library.Measurements.Models;

namespace SmartEnergy.Client.Components.Pages
{
    public partial class VariantG
    {
        string name = "Koen Smit";
        string variant = "G";
        int meterId = 15432909;
        private int tijdsPeriode = 1;
        //20s (20 seconds), 5m (5 minutes) or 1h (1 hour).
        string aggegationWindow = "1h";
        private double? calculatedPrice;

        private List<Measurement>? measurements;
        protected override async Task OnInitializedAsync()
        {
            int numberOfDays = 1;
            aggegationWindow = "1h";
            measurements = await this.measurementRepository.GetPower(meterId, numberOfDays, aggegationWindow);
        }

        //Method to calculate price
        private async Task setDays()
        {
            int numberOfDays = tijdsPeriode;
            aggegationWindow = "20s";
            measurements = await this.measurementRepository.GetPower(meterId, numberOfDays, aggegationWindow);
            calculatedPrice = CalculatePrice(measurements, aggegationWindow);
        }

        //calculate price, based on the measurements that are every HOUR
        private double? CalculatePrice(List<Measurement> measurements, string aggegationWindow)
        {
            double? totalPrice = 0;
            if (measurements == null)
            {
                return totalPrice;
            }
            foreach (var measurement in measurements)
            {
                // Skip measurements that have no value or energy price
                if (measurement.Value == null || measurement.EnergyPrice == null || measurement.Value == 0 || measurement.EnergyPrice == 0 || measurement.Value < 0)
                {
                    continue;
                }
                else
                {
                    // Convert the value to kiloWatt
                    double? kiloWattValue = measurement.Value / 1000;

                    // Verwerk de teruglevering
                    if (kiloWattValue < 0)
                    {
                        double? positiveKiloWattValue = Math.Abs(kiloWattValue.Value);
                        totalPrice -= measurement.EnergyPrice * positiveKiloWattValue;
                    }
                    else
                    {
                        // check if the timestamp is in the correct window, so the KWH can be calculated correctly(price is in euro per KWH)
                        if (aggegationWindow == "20s" && measurement.Timestamp.Second == 0)
                        {
                            //every 20 seconds
                            if (kiloWattValue < 0)
                            {
                                double? positiveKiloWattValue = Math.Abs(kiloWattValue.Value);
                                double? returnKwhValue = positiveKiloWattValue * (20.0 / 3600.0);
                                totalPrice -= measurement.EnergyPrice * returnKwhValue;
                                continue;
                            }
                            double? kwhValue = kiloWattValue * (20.0 / 3600.0);
                            totalPrice += measurement.EnergyPrice * kwhValue;
                        }
                        else if (aggegationWindow == "5m" && measurement.Timestamp.Second == 0 && measurement.Timestamp.Minute % 5 == 0)
                        {
                            //every 5 minutes
                            if (kiloWattValue < 0)
                            {
                                double? positiveKiloWattValue = Math.Abs(kiloWattValue.Value);
                                double? returnKwhValue = positiveKiloWattValue * (5.0 / 60.0);
                                totalPrice -= measurement.EnergyPrice * returnKwhValue;
                                continue;
                            }
                            double? kwhValue = kiloWattValue * (5.0 / 60.0);
                            totalPrice += measurement.EnergyPrice * kwhValue;
                        }
                        else if (aggegationWindow == "1h" && measurement.Timestamp.Minute == 0 && measurement.Timestamp.Second == 0)
                        {
                            //every hour
                            if (kiloWattValue < 0)
                            {
                                double? positiveKiloWattValue = Math.Abs(kiloWattValue.Value);
                                totalPrice -= measurement.EnergyPrice * positiveKiloWattValue;
                                continue;
                            }
                            totalPrice += measurement.EnergyPrice * kiloWattValue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            } // end foreach

            return totalPrice;
        }

        //Method to refresh data when clicked on the button
        private async Task RefreshData()
        {
            int numberOfDays = tijdsPeriode;
            aggegationWindow = "1h";
            measurements = await this.measurementRepository.GetPower(meterId, numberOfDays, aggegationWindow);
        }
    }
    

/*extra:
- maak meerdere huishoudens selecteerbaar
- grafische weergave van analyses
- other additions are allowed if justified
*/
}


