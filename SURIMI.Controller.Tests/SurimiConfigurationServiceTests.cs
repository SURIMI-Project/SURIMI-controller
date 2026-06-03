using Eii.BlobStore;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SURIMI_controller.Services;

namespace SURIMI_controller.ConfigurationService.Tests
{
    public class SurimiConfigurationServiceTests
    {
        private readonly string testYaml;
        private readonly Mock<ILogger<SurimiConfigurationService>> _loggerMock = new();
        private readonly Mock<IBlobStore> _blobStore = new();


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
      - market_code: ESALC
      - market_code: ESAQA
      - market_code: ESARN
      - market_code: ESBCN
      - market_code: ESBLA
      - market_code: ESBNI
      - market_code: ESBRX
      - market_code: ESCAR
      - market_code: ESCAS
      - market_code: ESCBL
      - market_code: ESCDK
      - market_code: ESCFE
      - market_code: ESCLP
      - market_code: ESDNA
      - market_code: ESERA
      - market_code: ESGAN
      - market_code: ESGLE
      - market_code: ESJAV
      - market_code: ESKLL
      - market_code: ESLAA
      - market_code: ESLLC
      - market_code: ESPAL
      - market_code: ESPNL
      - market_code: ESQAJ
      - market_code: ESROS
      - market_code: ESSAG
      - market_code: ESSCR
      - market_code: ESSFU
      - market_code: ESSPO
      - market_code: ESSPP
      - market_code: ESTAR
      - market_code: ESTOR
      - market_code: ESTRR
      - market_code: ESVJY
      - market_code: ESVLC
      - market_code: ESVLG
      - market_code: ESVZR
      - market_code: ESZME
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
      - species_code: BSK
      - species_code: CDI
      - species_code: CIL
      - species_code: COE
      - species_code: CVV
      - species_code: DAZ
      - species_code: DBO
      - species_code: DKH
      - species_code: DRR
      - species_code: DST
      - species_code: DTR
      - species_code: DTY
      - species_code: EOI
      - species_code: FAM
      - species_code: FBP
      - species_code: FPA
      - species_code: GLI
      - species_code: GRQ
      - species_code: HKE
        life_stage: juvenile
      - species_code: HKE
        life_stage: adult
      - species_code: HMM
      - species_code: HOM
      - species_code: HQB
      - species_code: ISY
      - species_code: JAA
      - species_code: JDP
      - species_code: JRS
      - species_code: LCW
      - species_code: LDB
      - species_code: LDV
      - species_code: LKO
      - species_code: LKT
      - species_code: LOW
      - species_code: LQZ
      - species_code: LVH
      - species_code: LXQ
      - species_code: MAC
      - species_code: MON
      - species_code: MOX
      - species_code: MTS
      - species_code: MUT
        life_stage: juvenile
      - species_code: MUT
        life_stage: adult
      - species_code: MVB
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
      - species_code: PIW
      - species_code: POD
      - species_code: RJC
      - species_code: RMM
      - species_code: SAA
      - species_code: SBA
      - species_code: SCK
      - species_code: SFS
      - species_code: SHO
      - species_code: SKM
      - species_code: SOO
      - species_code: SPC
      - species_code: SPR
      - species_code: SPW
      - species_code: SQE
      - species_code: SQM
      - species_code: SRG
      - species_code: SRJ
      - species_code: SYC
      - species_code: TSU
      - species_code: TTL
      - species_code: TVA
      - species_code: UIM
      - species_code: UYE
      - species_code: WHB
      fleet_segments:
      - gear_code: EwE:ART
        country_code: ESP
        model: EwE
      - gear_code: EwE:ART
        country_code: FRA
        model: EwE
      - gear_code: EwE:RECT
        country_code: ESP
        model: EwE
      - gear_code: LLS
        country_code: ESP
        model: EwE
      - gear_code: OTB
        country_code: ESP
        model: POSEIDON
      - gear_code: OTB
        country_code: FRA
        model: EwE
      - gear_code: PS
        country_code: ESP
        model: POSEIDON
      - gear_code: PS
        country_code: FRA
        model: EwE
      - gear_code: TM
        country_code: FRA
        model: EwE
    """;
        }

        [Fact]
        public void ReadYamlShouldReturnOK()
        {
            // Arrange
            var service = new SurimiConfigurationService(_loggerMock.Object, _blobStore.Object);

            // Act
            var conf = service.DeserialiseConfiguration(testYaml);

            // Assert
            conf.Should().NotBeNull();
            conf.Simulation.Should().NotBeNull();

        }

        public static TheoryData<string, DateTime, DateTime> Cases =
            new()
            {
                { "P1M",  new DateTime(2012, 12, 31, 23, 0, 0), new DateTime(2013, 1, 31, 23, 0, 0) },
                { "P1M", new DateTime(2013, 1, 31, 23, 0, 0), new DateTime(2013, 2, 28, 23, 0, 0) }
            };

        [Theory, MemberData(nameof(Cases))]
        public void TestSimulationManagerAddStepSize(
            string input,
            DateTime current,
            DateTime expectedDate)
        {
            // Arrange

            // Act
            var result = SimulationManager.AddStepSize(current, input);

            // Assert
            result.Should().Be(expectedDate);
        }

    }
}