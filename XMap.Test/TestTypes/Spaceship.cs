namespace XMap.Test.TestTypes
{
    using System;
    using System.Collections.Generic;

    public class Spaceship
    {
        public string Name { get; set; }
        public int Year { get; set; }
        public DateTime FirstLaunch { get; set; }
        public Agency Owner { get; set; }
        public List<Astronaut> Crew { get; set; }
        public Astronaut[] CrewArray { get; set; }
    }
}
