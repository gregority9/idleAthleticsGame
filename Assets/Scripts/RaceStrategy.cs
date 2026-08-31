namespace TrackDynasty.Mvp02
{
    public enum RaceStrategy
    {
        ExplosiveStart,
        Balanced,
        LatePush
    }

    public static class RaceStrategyInfo
    {
        public static string Title(RaceStrategy strategy)
        {
            if (strategy == RaceStrategy.ExplosiveStart) return "EXPLOSIVE START";
            if (strategy == RaceStrategy.LatePush) return "LATE PUSH";
            return "BALANCED";
        }

        public static string Description(RaceStrategy strategy)
        {
            if (strategy == RaceStrategy.ExplosiveStart) return "Fast opening, slightly weaker finish.";
            if (strategy == RaceStrategy.LatePush) return "Conserve early, stronger final 40m.";
            return "Reliable start and finish.";
        }
    }
}
