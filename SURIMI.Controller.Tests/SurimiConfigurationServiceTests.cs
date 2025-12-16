using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace SURIMI.ConfigurationService.Tests
{
    public class SurimiConfigurationServiceTests
    {
        private readonly string testYaml;
        private readonly Mock<ILogger<SurimiConfigurationService>> _loggerMock = new();

        public SurimiConfigurationServiceTests()
        {
            testYaml =
    """
    simulation:
      case_study_name: Western Med
      start_date_time: 2013-01-01T00:00:00
      maximum_end_date_time: 2024-01-01T00:00:00
      time_step: P1M
      geography:
        raster_cell_origin: Centroid
        crs:
          name: WGS 84
          authority: EPSG
          code: '4326'
        xres: 0.0833
        yres: 0.0833
        ncol: 109
        nrow: 82
        xmin: -1.0185
        xmax: 8.0612002
        ymin: 37.0139008
        ymax: 43.8445009
    standards:
      date_and_time: ISO 8601
      species_code: FAO 3-alpha
      gear_code: ISSCFG
      life_stage: dwc:lifeStage
      market_code: UN/LOCODE
      currency: ISO 4217
      country_code: ISO 3166-1 alpha-3
      measurements:
        system: SI
        units:
        - quantity: mass
          unit: kg
    items:
      markets:
      - market_code: ESSCR
      - market_code: ESAQA
      - market_code: ESCAS
      - market_code: ESCBL
      - market_code: ESTOR
      - market_code: ESVZR
      - market_code: ESBCN
      - market_code: ESTAR
      - market_code: ESCLP
      - market_code: ESROS
      - market_code: ESGAN
      - market_code: ESPAL
      - market_code: ESBRX
      - market_code: ESSPP
      - market_code: ESQAJ
      - market_code: ESDNA
      - market_code: ESPNL
      - market_code: ESARN
      - market_code: ESSPO
      - market_code: ESBLA
      - market_code: ESCAR
      - market_code: ESVLC
      - market_code: ESLLC
      - market_code: ESLAA
      - market_code: ESGLE
      - market_code: ESTRR
      - market_code: ESBNI
      - market_code: ESZME
      - market_code: ESSFU
      - market_code: ESSAG
      - market_code: ESVJY
      - market_code: ESJAV
      - market_code: ESERA
      - market_code: ESVLG
      - market_code: ESKLL
      - market_code: ESALC
      - market_code: ESCDK
      - market_code: ESCFE
      currencies:
      - currency_code: EUR
      species:
      - species_code: ALB
      - species_code: ANE
        life_stage: juvenile
      - species_code: ANE
        life_stage: adult
      - species_code: ANK
      - species_code: ARA
      - species_code: BFT
      - species_code: BIB
      - species_code: BLL
      - species_code: BOG
      - species_code: BON
      - species_code: BOY
      - species_code: BRF
      - species_code: BSH
      - species_code: CIL
      - species_code: COE
      - species_code: EOI
      - species_code: FAM
      - species_code: GLI
      - species_code: GRQ
      - species_code: HKE
        life_stage: juvenile
      - species_code: HKE
        life_stage: adult
      - species_code: HMM
      - species_code: HOM
      - species_code: HQB
      - species_code: IOD
      - species_code: JAA
      - species_code: JDP
      - species_code: JRS
      - species_code: LDB
      - species_code: LDV
      - species_code: LKO
      - species_code: LKT
      - species_code: MAC
      - species_code: MAZ
      - species_code: MON
      - species_code: MTS
      - species_code: MUT
        life_stage: juvenile
      - species_code: MUT
        life_stage: adult
      - species_code: NEP
      - species_code: OCC
      - species_code: OLV
      - species_code: OQT
      - species_code: OUL
      - species_code: OUM
      - species_code: PAC
      - species_code: PIL
        life_stage: juvenile
      - species_code: PIL
        life_stage: adult
      - species_code: POD
      - species_code: RJC
      - species_code: SAA
      - species_code: SBA
      - species_code: SCK
      - species_code: SFS
      - species_code: SHO
      - species_code: SKM
      - species_code: SLM
      - species_code: SPC
      - species_code: SPF
      - species_code: SQE
      - species_code: SQM
      - species_code: SRJ
      - species_code: SWO
      - species_code: SYC
      - species_code: TSU
      - species_code: WHB
      fleet_segments:
      - gear_code: OTB
        country_code: ESP
      - gear_code: PS
        country_code: ESP
      - gear_code: LLS
        country_code: ESP
      - gear_code: OTB
        country_code: FRA
      - gear_code: TM
        country_code: FRA
      - gear_code: PS
        country_code: FRA
    
    
    """;
        }

        [Fact]
        public void ReadYamlShouldReturnOK()
        {
            // Arrange
            var service = new SurimiConfigurationService(_loggerMock.Object);

            // Act
            var conf = service.DeserialiseConfiguration(testYaml);

            // Assert
            conf.Should().NotBeNull();
            conf.Simulation.Should().NotBeNull();

        }
    }
}