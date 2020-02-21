namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml.Serialization;

    ///<summary>
    /// Reads in controlled environment weather data and makes it available to models.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
    public class ControlledEnvironment : Model, IWeather
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link]
        private Clock clock = null;
        /// <summary>
        /// Gets the start date of the weather file
        /// </summary>
        public DateTime StartDate { get { return clock.StartDate; } }

        /// <summary>
        /// Gets the end date of the weather file
        /// </summary>
        public DateTime EndDate { get { return clock.EndDate;}}

        /// <summary>
        /// This event will be invoked immediately before models get their weather data.
        /// models and scripts an opportunity to change the weather data before other models
        /// reads it.
        /// </summary>
        public event EventHandler PreparingNewWeatherData;

        /// <summary>
        /// Gets or sets the maximum temperature (oC)
        /// </summary>
        [Description("Maximum Air Temperature (oC)")]
        public double MaxT { get; set; }

        /// <summary>
        /// Gets or sets the minimum temperature (oC)
        /// </summary>
        [Description("Minimum Air Temperature (oC)")]
        public double MinT { get; set; }

        /// <summary>
        /// Daily Mean temperature (oC)
        /// </summary>
        [Units("�C")]
        [XmlIgnore]
        public double MeanT { get { return (MaxT + MinT) / 2; } }

        /// <summary>
        /// Daily mean VPD (hPa)
        /// </summary>
        [Units("hPa")]
        [XmlIgnore]
        public double VPD
        {
            get
            {
                const double SVPfrac = 0.66;
                double VPDmint = MetUtilities.svp((float)MinT) - VP;
                VPDmint = Math.Max(VPDmint, 0.0);

                double VPDmaxt = MetUtilities.svp((float)MaxT) - VP;
                VPDmaxt = Math.Max(VPDmaxt, 0.0);

                return SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            }
        }

        /// <summary>
        /// Gets or sets the rainfall (mm)
        /// </summary>
        [Description("Rainfall (mm)")]        
        public double Rain { get; set; }

        /// <summary>
        /// Gets or sets the solar radiation. MJ/m2/day
        /// </summary>
        [Description("Solar Radiation (MJ/m2/d)")]
        public double Radn { get; set; }
		
        /// <summary>
        /// Gets or sets the Pan Evaporation (mm) (Class A pan)
        /// </summary>
        [Description("Pan Evaporation (mm)")]
        public double PanEvap { get; set; }

        /// <summary>
        /// Gets or sets the vapor pressure (hPa)
        /// </summary>
        [Description("Vapour Pressure (hPa)")]
        public double VP { get; set; }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        /// </summary>
        [Description("Wind Speed (m/s)")]
        public double Wind { get; set; }

        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        [Description("CO2 concentration of the air (ppm)")]
        public double CO2 { get; set; }

        /// <summary>
        /// Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.
        /// </summary>
        [Description("Air Pressure (hPa")]
        public double AirPressure { get; set; }

        /// <summary>
        /// Gets the latitude
        /// </summary>
        [Description("Latitude (deg)")]        
        public double Latitude{ get; set; }

        /// <summary>
        /// Gets the average temperature
        /// </summary>
        public double Tav { get { return (this.MinT + this.MaxT)/2; }}

        /// <summary>
        /// Gets the temperature amplitude.
        /// </summary>
        public double Amp { get {return 0;}}

        /// <summary>
        /// Gets the duration of the day in hours.
        /// </summary>
        [Description("Day Length (h)")]
        public double DayLength {get; set;}

        /// <summary>
        /// Calculate daylength using a given twilight angle
        /// </summary>
        /// <param name="twilight"></param>
        /// <returns></returns>
        public double CalculateDayLength(double twilight)
        {
            return DayLength;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ControlledEnvironment()
        {
            AirPressure = 1010;
        }

        /// <summary>
        /// An event handler for the daily DoWeather event.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            if (this.PreparingNewWeatherData != null)
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());
        }
    }
}