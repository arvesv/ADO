using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

var configuration = new ConfigurationBuilder()
  .AddUserSecrets<Program>()
  .Build();

// This program uses 2 secrets.
// ADO:PAT - Access token
// ADO:Org - Name of azure devops org


string personalAccessToken = configuration["ADO:PAT"]; //sl3i6omo73ieoecquewe2nfllsj56tnnnzm4t6ohv4jr7j3attbq;


using (var client = new HttpClient())
{
    client.BaseAddress = new Uri($"https://vssps.dev.azure.com/{configuration["ADO:Org"]}/");  //url of your organization
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    //encode your personal access token                   
    string credentials = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", personalAccessToken)));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

    string conttoken = "";
    //connect to the REST endpoint     

    HttpResponseMessage response;
    IEnumerable<string> continuationTokens = null;

    List<string> readersdesc = new List<string>();
    string qaDesc = "";

    bool morestuff = false;
    do {       
        if(string.IsNullOrEmpty(conttoken)) {
            response = client.GetAsync( "_apis/graph/groups?api-version=5.1-preview.1").Result;
        } else {
            response = client.GetAsync( $"_apis/graph/groups?continuationToken={conttoken}&api-version=5.1-preview.1").Result;          
        }

        if(!response.IsSuccessStatusCode) {
             throw(new Exception());
        }

        if(response.Headers.TryGetValues("x-ms-continuationtoken", out continuationTokens)) {
            conttoken = continuationTokens.FirstOrDefault();
            morestuff = true;
        }
        else 
        {
            morestuff = false;
        }

        var value = response.Content.ReadAsStringAsync().Result;
        JObject i = JObject.Parse(value);

        for(int z = 0; z < ((int)i["count"]); z++) {

            string name = ((string)i["value"][z]["displayName"]);


            if(name == "RDTeamQualityTools") {
                qaDesc = ((string)i["value"][z]["descriptor"]);
            }

            if(name == "Readers")
            {
                Console.WriteLine($"  Reader  {i["value"][z]["principalName"]}");

                readersdesc.Add((string)i["value"][z]["descriptor"]);
            }
        }
    } while(morestuff);


    foreach(var s in readersdesc) {

        var re = client.PutAsync($"_apis/graph/memberships/{qaDesc}/{s}?api-version=5.1-preview.1", null).Result;
    }




}