﻿using System.Collections.Generic;
using Terraria.ID;

namespace Aequus.Items.Accessories.CrownOfBlood {
    public partial class CrownOfBloodItem {
        public static readonly HashSet<int> NoBoost = new();

        private void LoadDataSets() {
            NoBoost.Add(ItemID.ShinyRedBalloon);
            NoBoost.Add(ItemID.BlizzardinaBalloon);
            NoBoost.Add(ItemID.BlueHorseshoeBalloon);
            NoBoost.Add(ItemID.BundleofBalloons);
            NoBoost.Add(ItemID.HoneyBalloon);
            NoBoost.Add(ItemID.CloudinaBalloon);
            NoBoost.Add(ItemID.FartInABalloon);
            NoBoost.Add(ItemID.WhiteHorseshoeBalloon);
            NoBoost.Add(ItemID.YellowHorseshoeBalloon);
            NoBoost.Add(ItemID.SharkronBalloon);
            NoBoost.Add(ItemID.TsunamiInABottle);
            NoBoost.Add(ItemID.BlizzardinaBottle);
            NoBoost.Add(ItemID.CloudinaBottle);
            NoBoost.Add(ItemID.SandstorminaBottle);
            NoBoost.Add(ItemID.FartinaJar);
            NoBoost.Add(ItemID.BalloonPufferfish);
            NoBoost.Add(ItemID.HorseshoeBundle);
            NoBoost.Add(ItemID.BalloonHorseshoeFart);
            NoBoost.Add(ItemID.BalloonHorseshoeHoney);
            NoBoost.Add(ItemID.BalloonHorseshoeSharkron);
            NoBoost.Add(ItemID.LuckyHorseshoe);
            NoBoost.Add(ItemID.ObsidianHorseshoe);
            NoBoost.Add(ItemID.SandstorminaBalloon);

            NoBoost.Add(ItemID.CelestialStone);
            NoBoost.Add(ItemID.CelestialShell);
            NoBoost.Add(ItemID.MoonShell);
            NoBoost.Add(ItemID.NeptunesShell);
            NoBoost.Add(ItemID.MoonCharm);
            NoBoost.Add(ItemID.SunStone);
            NoBoost.Add(ItemID.MoonStone);

            NoBoost.Add(ItemID.HermesBoots);
            NoBoost.Add(ItemID.RocketBoots);
            NoBoost.Add(ItemID.IceSkates);
            NoBoost.Add(ItemID.FloatingTube);
            NoBoost.Add(ItemID.WaterWalkingBoots);
            NoBoost.Add(ItemID.ObsidianWaterWalkingBoots);
            NoBoost.Add(ItemID.Flipper);
            NoBoost.Add(ItemID.FlyingCarpet);
            NoBoost.Add(ItemID.PortableStool);
            NoBoost.Add(ItemID.TigerClimbingGear);
            NoBoost.Add(ItemID.CopperWatch);
            NoBoost.Add(ItemID.TinWatch);
            NoBoost.Add(ItemID.SilverWatch);
            NoBoost.Add(ItemID.TungstenWatch);
            NoBoost.Add(ItemID.GoldWatch);
            NoBoost.Add(ItemID.PlatinumWatch);
            NoBoost.Add(ItemID.DepthMeter);
            NoBoost.Add(ItemID.Compass);
            NoBoost.Add(ItemID.Radar);
            NoBoost.Add(ItemID.LifeformAnalyzer);
            NoBoost.Add(ItemID.TallyCounter);
            NoBoost.Add(ItemID.MetalDetector);
            NoBoost.Add(ItemID.Stopwatch);
            NoBoost.Add(ItemID.DPSMeter);
            NoBoost.Add(ItemID.FishermansGuide);
            NoBoost.Add(ItemID.WeatherRadio);
            NoBoost.Add(ItemID.Sextant);
            NoBoost.Add(ItemID.GPS);
            NoBoost.Add(ItemID.REK);
            NoBoost.Add(ItemID.GoblinTech);
            NoBoost.Add(ItemID.FishFinder);
            NoBoost.Add(ItemID.PDA);
            NoBoost.Add(ItemID.MechanicalLens);
            NoBoost.Add(ItemID.LaserRuler);
            NoBoost.Add(ItemID.StarVeil);
            NoBoost.Add(ItemID.DiscountCard);
            NoBoost.Add(ItemID.LuckyCoin);
            NoBoost.Add(ItemID.CrossNecklace);
            NoBoost.Add(ItemID.AdhesiveBandage);
            NoBoost.Add(ItemID.Bezoar);
            NoBoost.Add(ItemID.ArmorPolish);
            NoBoost.Add(ItemID.Blindfold);
            NoBoost.Add(ItemID.FastClock);
            NoBoost.Add(ItemID.Megaphone);
            NoBoost.Add(ItemID.Nazar);
            NoBoost.Add(ItemID.Vitamins);
            NoBoost.Add(ItemID.TrifoldMap);
            NoBoost.Add(ItemID.ArmorBracing);
            NoBoost.Add(ItemID.MedicatedBandage);
            NoBoost.Add(ItemID.ThePlan);
            NoBoost.Add(ItemID.CountercurseMantra);
            NoBoost.Add(ItemID.PaladinsShield);
            NoBoost.Add(ItemID.BlackBelt);
            NoBoost.Add(ItemID.Tabi);
            NoBoost.Add(ItemID.HoneyComb);
            NoBoost.Add(ItemID.GravityGlobe);
            NoBoost.Add(ItemID.MasterNinjaGear);
            NoBoost.Add(ItemID.JellyfishNecklace);
            NoBoost.Add(ItemID.RifleScope);
            NoBoost.Add(ItemID.PanicNecklace);
            NoBoost.Add(ItemID.FrozenTurtleShell);
            NoBoost.Add(ItemID.ClothierVoodooDoll);
            NoBoost.Add(ItemID.GuideVoodooDoll);
            NoBoost.Add(ItemID.MagmaStone);
            NoBoost.Add(ItemID.ObsidianRose);
            NoBoost.Add(ItemID.SweetheartNecklace);
            NoBoost.Add(ItemID.FlurryBoots);
            NoBoost.Add(ItemID.SailfishBoots);
            NoBoost.Add(ItemID.HandWarmer);
            NoBoost.Add(ItemID.GoldRing);
            NoBoost.Add(ItemID.GreedyRing);
            NoBoost.Add(ItemID.CoinRing);
            NoBoost.Add(ItemID.FlowerBoots);
            NoBoost.Add(ItemID.CordageGuide);
            NoBoost.Add(ItemID.YoyoBag);
            NoBoost.Add(ItemID.YoYoGlove);
            NoBoost.Add(ItemID.BlackString);
            NoBoost.Add(ItemID.BlueString);
            NoBoost.Add(ItemID.BrownString);
            NoBoost.Add(ItemID.CyanString);
            NoBoost.Add(ItemID.GreenString);
            NoBoost.Add(ItemID.LimeString);
            NoBoost.Add(ItemID.OrangeString);
            NoBoost.Add(ItemID.PinkString);
            NoBoost.Add(ItemID.PurpleString);
            NoBoost.Add(ItemID.RainbowString);
            NoBoost.Add(ItemID.RedString);
            NoBoost.Add(ItemID.SkyBlueString);
            NoBoost.Add(ItemID.TealString);
            NoBoost.Add(ItemID.VioletString);
            NoBoost.Add(ItemID.WhiteString);
            NoBoost.Add(ItemID.YellowString);
            NoBoost.Add(ItemID.BlackCounterweight);
            NoBoost.Add(ItemID.BlueCounterweight);
            NoBoost.Add(ItemID.GreenCounterweight);
            NoBoost.Add(ItemID.PurpleCounterweight);
            NoBoost.Add(ItemID.RedCounterweight);
            NoBoost.Add(ItemID.YellowCounterweight);
            NoBoost.Add(ItemID.PhilosophersStone);
            NoBoost.Add(ItemID.SpectreBoots);
            NoBoost.Add(ItemID.SpectreGoggles);
            NoBoost.Add(ItemID.HellfireTreads);
            NoBoost.Add(ItemID.LavaFishingHook);
            NoBoost.Add(ItemID.StarCloak);
            NoBoost.Add(ItemID.CobaltShield);
            NoBoost.Add(ItemID.ObsidianShield);
            NoBoost.Add(ItemID.ObsidianSkull);
            NoBoost.Add(ItemID.ReflectiveShades);
            NoBoost.Add(ItemID.ShimmerCloak);

            NoBoost.Add(ItemID.FishingBobber);
            NoBoost.Add(ItemID.FishingBobberGlowingArgon);
            NoBoost.Add(ItemID.FishingBobberGlowingKrypton);
            NoBoost.Add(ItemID.FishingBobberGlowingViolet);
            NoBoost.Add(ItemID.FishingBobberGlowingXenon);
            NoBoost.Add(ItemID.FishingBobberGlowingRainbow);
            NoBoost.Add(ItemID.FishingBobberGlowingStar);
            NoBoost.Add(ItemID.FishingBobberGlowingLava);
            NoBoost.Add(ItemID.LavaproofTackleBag);
            NoBoost.Add(ItemID.TackleBox);
            NoBoost.Add(ItemID.AnglerTackleBag);
            NoBoost.Add(ItemID.AnglerEarring);
            NoBoost.Add(ItemID.HighTestFishingLine);
        }
    }
}