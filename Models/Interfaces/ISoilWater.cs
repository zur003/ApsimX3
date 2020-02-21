﻿namespace Models.Interfaces
{
    using Soils;

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface ISoilWater
    {
        ///<summary>Remove water from the profile</summary>
        void RemoveWater(double[] amountToRemove);

        ///<summary>Gets or sets soil thickness for each layer (mm)(</summary>
        double[] Thickness { get; }

        ///<summary>Gets or sets volumetric soil water content (mm/mm)(</summary>
        double[] SW { get; set; }

        ///<summary>Gets soil water content (mm)</summary>
        double[] SWmm { get; }

        ///<summary>Gets extractable soil water relative to LL15(mm)</summary>
        double[] ESW { get; }

        ///<summary>Gets potential evaporation from soil surface (mm)</summary>
        double Eos { get; }

        /// <summary>Gets the actual (realised) soil water evaporation (mm)</summary>
        double Es { get; }

        /// <summary>Gets potential evapotranspiration of the whole soil-plant system (mm)</summary>
        double Eo { get; set; }

        /// <summary>Gets the amount of water runoff (mm)</summary>
        double Runoff { get; }
        
        /// <summary>Gets the amount of water drainage from bottom of profile(mm)</summary>
        double Drainage { get; }

        /// <summary>Fraction of incoming radiation reflected from bare soil</summary>
        double Salb { get; }

        /// <summary>Amount of water moving laterally out of the profile (mm)</summary>
        double[] LateralOutflow { get; }

        /// <summary>Amount of N leaching as NO3-N from the deepest soil layer (kg /ha)</summary>
        double LeachNO3 { get; }

        /// <summary>Amount of N leaching as NH4-N from the deepest soil layer (kg /ha)</summary>
        double LeachNH4 { get; }
        
        /// <summary>Amount of N leaching as urea-N  from the deepest soil layer (kg /ha)</summary>
        double LeachUrea { get; }

        /// <summary>Amount of N leaching as NO3 from each soil layer (kg /ha)</summary>
        double[] FlowNO3 { get; }

        /// <summary>Amount of N leaching as NH4 from each soil layer (kg /ha)</summary>
        double[] FlowNH4 { get; }

        /// <summary>Amount of N leaching as urea from each soil layer (kg /ha)</summary>
        double[] FlowUrea { get; }

        /// <summary>Amount of water moving upward from each soil layer during unsaturated flow (negative value means downward movement) (mm)</summary>
        double[] Flow { get; }

        /// <summary>Amount of water moving downward out of each soil layer due to gravity drainage (above DUL) (mm)</summary>
        double[] Flux { get; }

        /// <summary> This is set by Microclimate and is rainfall less that intercepted by the canopy and residue components </summary>
        double PotentialInfiltration { get; set; }
        
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        double PrecipitationInterception { get; set; }

        /// <summary>Sets the water table.</summary>
        /// <param name="InitialDepth">The initial depth.</param>
        void SetWaterTable(double InitialDepth);

        ///<summary>Perform a reset</summary>
        void Reset();

        ///<summary>Perform tillage</summary>
        void Tillage(TillageType Data);

        ///<summary>Perform tillage</summary>
        void Tillage(string tillageType);
    }
}
