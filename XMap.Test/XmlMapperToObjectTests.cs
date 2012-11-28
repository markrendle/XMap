namespace XMap.Test
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using TestTypes;
    using XMap;
    using Xunit;

    public class XmlMapperToObjectTests
    {
        [Fact]
        public void SetsStringProperty()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"name", o => o.Name},
                             };

            var xml = XElement.Parse(@"<spaceship name=""Apollo 11""/>");
            var actual = mapper.ToObject(xml);

            Assert.Equal("Apollo 11", actual.Name);
        }
        
        [Fact]
        public void SetsInt32Property()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"year", o => o.Year},
                             };

            var xml = XElement.Parse(@"<spaceship year=""1969""/>");
            var actual = mapper.ToObject(xml);

            Assert.Equal(1969, actual.Year);
        }

        [Fact]
        public void SetsStringAndInt32Property()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"name", o => o.Name},
                                 {"year", o => o.Year},
                             };

            var xml = XElement.Parse(@"<spaceship name=""Apollo 11"" year=""1969""/>");
            var actual = mapper.ToObject(xml);

            Assert.Equal("Apollo 11", actual.Name);
            Assert.Equal(1969, actual.Year);
        }

        [Fact]
        public void SetsDateTimePropertyUsingCustomConverter()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"launchDate", o => o.FirstLaunch, s => DateTime.Parse(s)},
                             };
            var xml = XElement.Parse(@"<spaceship launchDate=""16/7/1969""/>");
            var actual = mapper.ToObject(xml);

            Assert.Equal(new DateTime(1969,7,16), actual.FirstLaunch);
        }

        [Fact]
        public void SetsStringAndInt32AndDateTimeProperty()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"name", o => o.Name},
                                 {"year", o => o.Year},
                                 {"launchDate", o => o.FirstLaunch, s => DateTime.Parse(s)},
                             };

            var xml = XElement.Parse(@"<spaceship name=""Apollo 11"" year=""1969"" launchDate=""16/7/1969""/>");

            var actual = mapper.ToObject(xml);

            Assert.Equal("Apollo 11", actual.Name);
            Assert.Equal(1969, actual.Year);
            Assert.Equal(new DateTime(1969,7,16), actual.FirstLaunch);
        }

        [Fact]
        public void SetsIndirectStringProperty()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"agencyName", o => o.Owner.Name},
                             };

            var xml = XElement.Parse(@"<spaceship agencyName=""NASA""/>");

            var actual = mapper.ToObject(xml);

            Assert.Equal("NASA", actual.Owner.Name);
        }
        
        [Fact]
        public void SetsIndirectEnumProperty()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"agencyCountry", o => o.Owner.Country},
                             };

            var xml = XElement.Parse(@"<spaceship agencyCountry=""USA""/>");

            var actual = mapper.ToObject(xml);

            Assert.Equal(Country.USA, actual.Owner.Country);
        }

        [Fact]
        public void DoesNotOverwriteExistingObject()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"agencyName", o => o.Owner.Name},
                             };

            var xml = XElement.Parse(@"<spaceship agencyName=""NASA""/>");

            var original = new Spaceship {Owner = new Agency {Country = Country.USA}};
            var actual = mapper.ToObject(xml, original);

            Assert.Equal("NASA", actual.Owner.Name);
            Assert.Equal(Country.USA, actual.Owner.Country);
        }

        [Fact]
        public void SetsComplexPropertyFromElement()
        {
            var agencyMapper = new XmlMapper<Agency>
                                   {
                                       {"name", a => a.Name},
                                       {"country", a => a.Country},
                                   };

            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"agency", o => o.Owner, agencyMapper},
                             };

            var xml = XElement.Parse(@"<spaceship><agency name=""NASA"" country=""USA""/></spaceship>");

            var actual = mapper.ToObject(xml);

            Assert.Equal(Country.USA, actual.Owner.Country);
        }

        [Fact]
        public void SetsCollectionFromElement()
        {
            var astronautMapper = new XmlMapper<Astronaut>
                                      {
                                          {"name", a => a.Name},
                                      };

            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"crew/member", o => o.Crew, astronautMapper},
                             };

            var xml = XElement.Parse(@"<spaceship><crew><member name=""Buzz Aldrin""/><member name=""Neil Armstrong""/></crew></spaceship>");

            var actual = mapper.ToObject(xml);

            Assert.Equal(2, actual.Crew.Count);
            Assert.Contains("Buzz Aldrin", actual.Crew.Select(c => c.Name));
            Assert.Contains("Neil Armstrong", actual.Crew.Select(c => c.Name));
        }

        [Fact]
        public void SetsPropertyFromTwoElements()
        {
            var mapper = new XmlMapper<Astronaut>
                             {
                                 {"fname", "lname", a => a.Name, (f,l) => string.Format("{0} {1}", f, l), s => Tuple.Create("","")},
                             };
            var xml = XElement.Parse(@"<astronaut fname=""Michael"" lname=""Collins""/>");

            var actual = mapper.ToObject(xml);

            Assert.Equal("Michael Collins", actual.Name);
        }

        [Fact]
        public void SetsCollectionItemFromElement()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"captainName", o => o.Crew[0].Name},
                                 {"pilotName", o => o.Crew[1].Name},
                             };

            var xml = XElement.Parse(@"<spaceship captainName=""Neil Armstrong"" pilotName=""Michael Collins""/>");

            var actual = mapper.ToObject(xml);

            Assert.Equal(2, actual.Crew.Count);
            Assert.Equal("Neil Armstrong", actual.Crew[0].Name);
            Assert.Equal("Michael Collins", actual.Crew[1].Name);
        }
        
        [Fact]
        public void SetsArrayItemFromElement()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"captainName", o => o.CrewArray[0].Name},
                                 {"pilotName", o => o.CrewArray[1].Name},
                             };

            var xml = XElement.Parse(@"<spaceship captainName=""Neil Armstrong"" pilotName=""Michael Collins""/>");

            var actual = mapper.ToObject(xml);

            Assert.Equal(2, actual.CrewArray.Length);
            Assert.Equal("Neil Armstrong", actual.CrewArray[0].Name);
            Assert.Equal("Michael Collins", actual.CrewArray[1].Name);
        }
    }
}
