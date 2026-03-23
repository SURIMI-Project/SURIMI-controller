namespace SurimiGUI.Models
{
    public class EditoApp
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        // Add other properties as needed based on the actual API response
    }

    public class Rootobject
    {
        public App[] apps { get; set; }
        public object[] groups { get; set; }
    }

    public class App
    {
        public string id { get; set; }
        public string name { get; set; }
        public int instances { get; set; }
        public float cpus { get; set; }
        public float mem { get; set; }
        public string status { get; set; }
        public string[] urls { get; set; }
        public Env env { get; set; }
        public Task[] tasks { get; set; }
        public string subtitle { get; set; }
        public string postInstallInstructions { get; set; }
        public string _namespace { get; set; }
        public string revision { get; set; }
        public string updated { get; set; }
        public string appVersion { get; set; }
        public string chart { get; set; }
        public long startedAt { get; set; }
        public bool suspendable { get; set; }
        public bool suspended { get; set; }
        public string catalogId { get; set; }
        public string owner { get; set; }
        public string friendlyName { get; set; }
        public bool share { get; set; }
        public Controller[] controllers { get; set; }
    }

    public class Env
    {
        public string catalogType { get; set; }
        public string networkinguserport { get; set; }
        public string gitbranch { get; set; }
        public string gitenabled { get; set; }
        public string initpersonalInitArgs { get; set; }
        public string gittoken { get; set; }
        public string gitname { get; set; }
        public string ingressenabled { get; set; }
        public string serviceimagecustomversion { get; set; }
        public string ingresscertManagerClusterIssuer { get; set; }
        public string ingresshostname { get; set; }
        public string ingressuserHostname { get; set; }
        public string securityallowlistip { get; set; }
        public string nodeSelectormagnumopenstackorgnodegroup { get; set; }
        public string resourceslimitsmemory { get; set; }
        public string globalsuspend { get; set; }
        public string ingressingressClassName { get; set; }
        public string openshiftSCCenabled { get; set; }
        public string startupProbesuccessThreshold { get; set; }
        public string securityallowlistenabled { get; set; }
        public string routeenabled { get; set; }
        public string gitcache { get; set; }
        public string gitrepository { get; set; }
        public string resourcesrequestsmemory { get; set; }
        public string persistencesize { get; set; }
        public string vaultsecret { get; set; }
        public string routeuserHostname { get; set; }
        public string vaultenabled { get; set; }
        public string ingressuseCertManager { get; set; }
        public string resourcesrequestscpu { get; set; }
        public string startupProbefailureThreshold { get; set; }
        public string vaultdirectory { get; set; }
        public string startupProbeperiodSeconds { get; set; }
        public string serviceimagecustomenabled { get; set; }
        public string userPreferencesdarkMode { get; set; }
        public string openshiftSCCscc { get; set; }
        public string s3bucketName { get; set; }
        public string vaulttoken { get; set; }
        public string startupProbeinitialDelaySeconds { get; set; }
        public string s3defaultRegion { get; set; }
        public string s3endpoint { get; set; }
        public string repositorypipRepository { get; set; }
        public string vaultmount { get; set; }
        public string resourceslimitscpu { get; set; }
        public string serviceimagepullPolicy { get; set; }
        public string s3enabled { get; set; }
        public string s3sessionToken { get; set; }
        public string userPreferenceslanguage { get; set; }
        public string serviceimageversion { get; set; }
        public string networkinguserenabled { get; set; }
        public string vaulturl { get; set; }
        public string s3secretAccessKey { get; set; }
        public string securitynetworkPolicyenabled { get; set; }
        public string repositorycondaRepository { get; set; }
        public string routehostname { get; set; }
        public string s3accessKeyId { get; set; }
        public string gitemail { get; set; }
        public string persistenceenabled { get; set; }
        public string startupProbetimeoutSeconds { get; set; }
        public string initpersonalInit { get; set; }
        public string securitypassword { get; set; }
        public string discoverymetaflow { get; set; }
        public string userPreferencesaiAssistantapiBase { get; set; }
        public string certificatescacerts { get; set; }
        public string discoverychromadb { get; set; }
        public string kubernetesrole { get; set; }
        public string kubernetesenabled { get; set; }
        public string proxyenabled { get; set; }
        public string userPreferencesaiAssistantembeddingsProvider { get; set; }
        public string messagefr { get; set; }
        public string discoverymilvus { get; set; }
        public string initregionInit { get; set; }
        public string certificatespathToCaBundle { get; set; }
        public string proxyhttpProxy { get; set; }
        public string userPreferencesaiAssistantapiKey { get; set; }
        public string messageen { get; set; }
        public string discoverypostgresql { get; set; }
        public string discoverymlflow { get; set; }
        public string userPreferencesaiAssistantmodelProvider { get; set; }
        public string proxyhttpsProxy { get; set; }
        public string proxynoProxy { get; set; }
        public string userPreferencesaiAssistantenabled { get; set; }
        public string userPreferencesaiAssistantmodel { get; set; }
        public string discoveryhive { get; set; }
        public string suspend { get; set; }
        public string otelPropertiesotelUrl { get; set; }
        public string modelUrlsposeidonUrl { get; set; }
        public string excludeServicesexcludeAggregator { get; set; }
        public string modelUrlscmsyUrl { get; set; }
        public string excludeServicesexcludeValuechain { get; set; }
        public string modelUrlsvaluechainUrl { get; set; }
        public string excludeServicesexcludeCms { get; set; }
        public string modelUrlsmarketUrl { get; set; }
        public string modelUrlsecopathUrl { get; set; }
        public string copernicusMarinepassword { get; set; }
        public string copernicusMarineusername { get; set; }
        public string containerUrlscontrollerUrl { get; set; }
        public string copernicusMarineenabled { get; set; }
    }

    public class Task
    {
        public string id { get; set; }
        public Status status { get; set; }
        public Container[] containers { get; set; }
    }

    public class Status
    {
        public string status { get; set; }
    }

    public class Container
    {
        public string name { get; set; }
        public bool ready { get; set; }
    }

    public class Controller
    {
        public bool healthy { get; set; }
        public string name { get; set; }
        public string kind { get; set; }
        public Details details { get; set; }
    }

    public class Details
    {
        public int desired { get; set; }
        public int ready { get; set; }
    }
}

