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
        private List<double?> calculatedPrice = new List<double?> { 0, 0, 0 };
        double? totalPriceWithSurcharge = 0;
        double? totalPriceWithoutSurcharge = 0;
        double? averageSurcharge = 0;
        double? totalUsage = 0;
        double? totalPriceWithSurchargeToday = 0;
        double? totalPriceWithoutSurchargeToday = 0;
        double? averageSurchargeToday = 0;
        double? totalUsageToday = 0;
        int numberOfDays = 1;

        private List<Measurement>? measurements;
        protected override async Task OnInitializedAsync()
        {
            int numberOfDays = 1;
            aggegationWindow = "1h";
            measurements = await this.measurementRepository.GetPower(meterId, numberOfDays, aggegationWindow);
            calculatedPrice = CalculatePrice(measurements, aggegationWindow);
            totalPriceWithSurchargeToday = calculatedPrice[0];
            totalPriceWithoutSurchargeToday = calculatedPrice[1];
            totalUsageToday = calculatedPrice[2];
            averageSurchargeToday = calculatedPrice[3];
        }

        //stel het aantal dagen in, roep methode aan om de prijs te berekenen
        private async Task setDays()
        {
            // zorgt er voor dat je front-end niet de slider kan aanpassen, zo heb je geen rare resultaten of errors
            if (tijdsPeriode >= 1 && tijdsPeriode <= 30)
            {
                numberOfDays = tijdsPeriode;
            }
            else{
                tijdsPeriode = 1;
                numberOfDays = tijdsPeriode;
            }
            //roep de data op voor het aantal geselecteerde dagen
            aggegationWindow = "1h";
            measurements = await this.measurementRepository.GetPower(meterId, numberOfDays, aggegationWindow);
            //bereken de prijs, totale value en prijs zonder toeslag
            calculatedPrice = CalculatePrice(measurements, aggegationWindow);
            totalPriceWithSurcharge = calculatedPrice[0];
            totalPriceWithoutSurcharge = calculatedPrice[1];
            totalUsage = calculatedPrice[2];
            averageSurcharge = calculatedPrice[3];
        }

        //bereken de prijs, totale value en prijs zonder toeslag
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

            //Als de aggregationWindow niet per uur gaat, dan zou de berekening niet meer kloppen.
            double timeFactor;
            switch (aggregationWindow)
            {
                case "20s":
                    timeFactor = 20.0 / 3600.0;
                    break;
                case "5m":
                    timeFactor = 5.0 / 60.0;
                    break;
                case "1h":
                    timeFactor = 1.0;
                    break;
                default:
                    return new List<double?> { totalPrice, totalPriceNoAddition, totalUsage, totalSurchargePercentage };
            }

            // Parallel.ForEach word gebruikt om meerder taken tegelijk uit te voeren, zo is de data sneller binnen
            Parallel.ForEach(measurements, (measurement) =>
            {
                // geen data = geen berekening
                if (measurement.Value == null || measurement.EnergyPrice == null || measurement.Value <= 0 || measurement.EnergyPrice <= 0)
                {
                    return;
                }

                //zet om naar kiloWatt
                double kiloWattValue = measurement.Value.Value / 1000;
                double wattValue = measurement.Value.Value;

                // check of de timewindow ook echt klopt
                if ((aggregationWindow == "20s" && measurement.Timestamp.Second != 0) || 
                    (aggregationWindow == "5m" && (measurement.Timestamp.Second != 0 || measurement.Timestamp.Minute % 5 != 0)) || 
                    aggregationWindow == "1h" && (measurement.Timestamp.Minute != 0 || measurement.Timestamp.Second != 0)) 
                    return;

                double surchargePercentage = 0;

                if (wattValue > 4000)
                {
                    surchargePercentage = 0.47; // 47%
                }
                else if (wattValue > 3000)
                {
                    surchargePercentage = 0.44; // 44%
                }
                else if (wattValue > 2000)
                {
                    surchargePercentage = 0.38; // 38%
                }
                else if (wattValue > 1000)
                {
                    surchargePercentage = 0.25; // 25%
                }

                double energyPriceWithoutSurcharge = measurement.EnergyPrice.Value;
                double adjustedEnergyPrice = energyPriceWithoutSurcharge * (1 + surchargePercentage);

                double priceChangeWithoutSurcharge;
                double priceChangeWithSurcharge;
                // bereken de prijs, als het onder 0 is dan moet het van de totaal prijs worden afgetrokken
                if (kiloWattValue < 0)
                {
                    priceChangeWithoutSurcharge = -(energyPriceWithoutSurcharge * Math.Abs(kiloWattValue) * timeFactor);
                    priceChangeWithSurcharge = -(adjustedEnergyPrice * Math.Abs(kiloWattValue) * timeFactor);
                }
                else
                {
                    priceChangeWithoutSurcharge = energyPriceWithoutSurcharge * kiloWattValue * timeFactor;
                    priceChangeWithSurcharge = adjustedEnergyPrice * kiloWattValue * timeFactor;
                }

                //lock zorgt er voor dat er maar 1 stukje per keer kan worden uitgevoerd, omdat ik Parallel gebruik. anders krijg je rare resultaten
                lock (lockObject)
                {
                    totalUsage += kiloWattValue;
                    totalPriceNoAddition += priceChangeWithoutSurcharge;
                    totalPrice += priceChangeWithSurcharge;

                    if (surchargePercentage > 0)
                    {
                        totalSurchargePercentage += surchargePercentage;
                        surchargeCount++;
                    }
                }
            });
            // bereken gemiddelde toeslag en zet het om naar percentage
            double averageSurchargePercentage = surchargeCount > 0 ? totalSurchargePercentage / surchargeCount : 0;
            averageSurchargePercentage = averageSurchargePercentage * 100; 

            //rond af
            totalUsage = Math.Round(totalUsage, 2);

            return new List<double?> { totalPrice, totalPriceNoAddition, totalUsage, averageSurchargePercentage };
        }
    }
}


