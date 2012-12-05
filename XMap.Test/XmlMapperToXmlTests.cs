namespace XMap.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using TestTypes;
    using XMap;
    using Xunit;

    public class XmlMapperToXmlTests
    {
        [Fact]
        public void CreatesRootElement()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"name", o => o.Name},
                             };

            var obj = new Spaceship {Name = "Apollo 11"};

            var actual = mapper.ToXml(obj, "spaceship");

            Assert.Equal("spaceship", actual.Name);
        }
        
        [Fact]
        public void SetsStringAttribute()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"name", o => o.Name},
                             };

            var obj = new Spaceship {Name = "Apollo 11"};

            var actual = mapper.ToXml(obj, "spaceship");

            Assert.Equal("Apollo 11", actual.Attribute("name").Value);
        }
        
        [Fact]
        public void SetsIntAttribute()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"year", o => o.Year},
                             };

            var obj = new Spaceship {Year = 1969};

            var actual = mapper.ToXml(obj, "spaceship");

            Assert.Equal("1969", actual.Attribute("year").Value);
        }
        
        [Fact]
        public void SetsDateTimeAttribute()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"firstLaunch", o => o.FirstLaunch},
                             };

            var obj = new Spaceship { FirstLaunch = new DateTime(1969, 7, 16) };

            var actual = mapper.ToXml(obj, "spaceship");

            Assert.Equal(obj.FirstLaunch.ToString(CultureInfo.CurrentCulture), actual.Attribute("firstLaunch").Value);
        }
        
        [Fact]
        public void SetsDateTimeAttributeWithJustCustomParser()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"firstLaunch", o => o.FirstLaunch, s => DateTime.Parse(s), p => p.ToString("yyyy-MM-dd")},
                             };

            var obj = new Spaceship { FirstLaunch = new DateTime(1969, 7, 16) };

            var actual = mapper.ToXml(obj, "spaceship");

            Assert.Equal("1969-07-16", actual.Attribute("firstLaunch").Value);
        }

        [Fact]
        public void SetsAttributeFromIndirectStringProperty()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"agencyName", o => o.Owner.Name},
                             };

            var spaceship = new Spaceship
                                {
                                    Owner = new Agency
                                                {
                                                    Name = "NASA"
                                                }
                                };

            var actual = mapper.ToXml(spaceship, "spaceship");

            Assert.Equal("NASA", actual.Attribute("agencyName").Value);
        }

        [Fact]
        public void SetsAttributesFromCollectionItems()
        {
            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"captainName", o => o.Crew[0].Name},
                                 {"pilotName", o => o.Crew[1].Name},
                             };

            var spaceship = new Spaceship
                                {
                                    Crew = new List<Astronaut>
                                               {
                                                   new Astronaut {Name = "Neil Armstrong"},
                                                   new Astronaut {Name = "Michael Collins"},
                                               }
                                };

            var actual = mapper.ToXml(spaceship, "spaceship");

            Assert.Equal("Neil Armstrong", actual.Attribute("captainName").Value);
            Assert.Equal("Michael Collins", actual.Attribute("pilotName").Value);
        }

        [Fact]
        public void SetsElementFromComplexProperty()
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

            var spaceship = new Spaceship
                                {
                                    Owner = new Agency
                                                {
                                                    Name = "NASA",
                                                    Country = Country.USA
                                                }
                                };

            var actual = mapper.ToXml(spaceship, "spaceship");

            var agency = actual.Element("agency");
            Assert.NotNull(agency);
            Assert.Equal("NASA", agency.Attribute("name").Value);
        }

        [Fact]
        public void SetsElementsFromCollection()
        {
            var astronautMapper = new XmlMapper<Astronaut>
                                      {
                                          {"name", a => a.Name},
                                      };

            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"crew/member", o => o.Crew, astronautMapper},
                             };

            var spaceship = new Spaceship
                                {
                                    Crew = new List<Astronaut>
                                               {
                                                   new Astronaut {Name = "Buzz Aldrin"},
                                                   new Astronaut {Name = "Neil Armstrong"},
                                               }
                                };


            var actual = mapper.ToXml(spaceship, "spaceship");

            var crew = actual.Element("crew");
            Assert.NotNull(crew);
            Assert.Equal(2, crew.Elements("member").Count());

            Assert.Contains("Buzz Aldrin", crew.Elements("member").Select(x => x.Attribute("name").Value));
            Assert.Contains("Neil Armstrong", crew.Elements("member").Select(x => x.Attribute("name").Value));
        }

        [Fact]
        public void SetsTwoAttributesFromProperty()
        {
            var mapper = new XmlMapper<Astronaut>
                             {
                                 {"fname", "lname", a => a.Name, (f,l) => string.Format("{0} {1}", f, l), s => Split(s)},
                             };

            var astronaut = new Astronaut {Name = "Michael Collins"};

            var actual = mapper.ToXml(astronaut, "astronaut");

            Assert.Equal("Michael", actual.Attribute("fname").Value);
            Assert.Equal("Collins", actual.Attribute("lname").Value);
        }

        [Fact]
        public void CreateElementFromEmptyCollection()
        {
            var astronautMapper = new XmlMapper<Astronaut>
                                      {
                                          {"name", a => a.Name},
                                      };

            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"crew/member", o => o.Crew, astronautMapper},
                             };

            var spaceship = new Spaceship
                                {
                                    Crew = new List<Astronaut>(),
                                };

            var actual = mapper.ToXml(spaceship, "spaceship");

            Assert.Equal("<crew />", actual.Element("crew").ToString());

        }

        [Fact]
        public void CreateElementFromNullCollection()
        {
            var astronautMapper = new XmlMapper<Astronaut>
                                      {
                                          {"name", a => a.Name},
                                      };

            var mapper = new XmlMapper<Spaceship>
                             {
                                 {"crew/member", o => o.Crew, astronautMapper},
                             };

            var spaceship = new Spaceship
            {
                Crew = null,
            };

            var actual = mapper.ToXml(spaceship, "spaceship");

            Assert.Equal("<crew />", actual.Element("crew").ToString());
        }

        [Fact]
        public void CreateElementFromNullObject()
        {
            var astronautMapper = new XmlMapper<Astronaut>
                                      {
                                          {"name", a => a.Name},
                                      };

            Astronaut astronaut = null;

            var actual = astronautMapper.ToXml(astronaut, "astronaut");

            Assert.Equal("<astronaut />", actual.ToString());
        }

        static Tuple<string, string> Split(string source)
        {
            var bits = source.Split(' ');
            return Tuple.Create(string.Join(" ", bits.Reverse().Skip(1).Reverse()), bits.Last());
        }
    }
}