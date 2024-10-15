using SmartEnergy.Library.Measurements.Models;

namespace SmartEnergy.Client.Components.Pages
{
    public partial class VariantG
    {
        string name = "Koen Smit";
        string variant = "G";


        private List<Measurement>? measurements;
        protected override async Task OnInitializedAsync()
        {
            /* TODO: update the value to the ID of your meter. Please note that this meterId is a decimal number and your
            P1 meter is a Hexadecimal number. Thus you need to convert the ID from HEX to Decimal and put the number here. */
            int meterId = 15432909;
            //1458270

            /* Number of days to retrieve*/
            int numberOfDays = 3;

            /*The time window to summarize the data, example: 20s (20 seconds), 5m (5 minutes) or 1h (1 hour). */
            string aggegationWindow = "5m";

            /* there are multiple data streams available. Two for the value of the power meter (consumed, produced)
            one for the gas consumption and another one for the absolute power usage. All of the possible method calls:*/
            measurements = await this.measurementRepository.GetEnergyConsumed(meterId, numberOfDays, aggegationWindow);
            //measurements = await this.measurementRepository.GetEnergyProduced(meterId, numberOfDays, aggegationWindow);
            //measurements = await this.measurementRepository.GetPower(meterId, numberOfDays, aggegationWindow);
        }

        private async Task RefreshData()
        {
            int meterId = 15432909;
            int numberOfDays = 3;
            string aggegationWindow = "5m";
            measurements = await this.measurementRepository.GetEnergyConsumed(meterId, numberOfDays, aggegationWindow);
        }
    }
    

/*extra:
- maak meerdere huishoudens selecteerbaar
- grafische weergave van analyses
- other additions are allowed if justified
*/
}


