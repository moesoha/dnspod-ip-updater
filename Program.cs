using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DNSPodUpdater{
	class Program{
		private static JObject _config;
		
		private static bool keepRunning=true;

		private static string lastIP="255.255.255.255";
		private static string toUpdate_DomainID;
		private static ArrayList toUpdate_RecordID=new ArrayList();

		static void Main(string[] args){
			string configLocation="./DNSPodUpdater.json";
			if(args.Length>0){
				switch(args[0]){
					case "-h":
						System.Console.WriteLine(
							"Hey! This is DNSPod IP Updater! \n"+
							"\nUsage:\n"+
							"DNSPodUpdater.exe [config=./DNSPodUpdater.json]"
						);
						System.Environment.Exit(0);
						break;
					default:
						configLocation=args[0];
						break;
				}
			}
			loadConfig(configLocation);
			Updater();
			
			Console.CancelKeyPress+=delegate(object sender,ConsoleCancelEventArgs e){
				e.Cancel=true;
				keepRunning=false;
			};
			while(keepRunning){}
			Console.WriteLine("Bye!");
		}

		private static async void Updater(){
			JObject domainInfo=await getDomainInfo(_config["domain"].Value<string>());
			toUpdate_DomainID=domainInfo["id"].Value<string>();
			Console.WriteLine("Domain ID: "+toUpdate_DomainID);

			foreach(JValue row in _config["subdomain"]){
				insertRecordIdsFromJObject(await findSubDomainInRecordList(row.Value<string>(),toUpdate_DomainID));
			}
			/*foreach(Object row in toUpdate_RecordID){
				Console.WriteLine("SubDomain ID:"+(string)row);
			}*/
			string recordIds=string.Join(", ",toUpdate_RecordID.Cast<string>().ToArray());
			Console.WriteLine("SubDomain ID:"+recordIds);

			while(keepRunning){
				string ip="";
				bool ok=true;
				try{
					ip=await getIP();
				}catch{
					ok=false;
					Console.WriteLine("Error Caught while get ip!");
				}
				if(ok && (ip!=lastIP)){
					Console.WriteLine("IP("+ip+") is different from last check("+lastIP+")! Updating Records...");
					JObject job=await BatchUpdateIP(recordIds,ip);
					if(job["status"]["code"].Value<string>()=="1"){
						Console.WriteLine("Updating job was added. JobID is "+job["job_id"].Value<string>());
						lastIP=ip;
					}else{
						Console.WriteLine("Job was not added:"+job["status"]["message"].Value<string>());
					}
					
				}
				System.Threading.Thread.Sleep(10000);
			}
		}

		private static void loadConfig(string configLocation){
			string jsonconfig=System.IO.File.ReadAllText(configLocation);
			_config=JObject.Parse(jsonconfig);
		}

		private static void insertRecordIdsFromJObject(JArray jo){
			foreach(JObject row in jo){
				if((row["type"].Value<string>()=="A") && row["enabled"].Value<string>()=="1"){
					toUpdate_RecordID.Add(row["id"].Value<string>());
				}
			}
		}

		private static Task<string> getIP(){
			return(Task.Run(()=>{
				using(var _client=new HttpClient(new HttpClientHandler{AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate},false)){
					//_client.BaseAddress=new Uri("http://ns1.dnspod.net:6666/");
					//HttpResponseMessage response=_client.GetAsync("/").Result;
					_client.BaseAddress=new Uri("http://ifconfig.me/");
					HttpResponseMessage response=_client.GetAsync("ip").Result;
					response.EnsureSuccessStatusCode();
					return(response.Content.ReadAsStringAsync().Result.Trim());
				}
			}));
		}

		private static Task<JObject> getDomainInfo(string domainname){
			return(Task.Run(()=>{
				using(var _client=new HttpClient(new HttpClientHandler{AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate},false)){
					_client.BaseAddress=new Uri("https://dnsapi.cn/");

					var content=new FormUrlEncodedContent(new[]{
						new KeyValuePair<string, string>("login_token",_config["token"].Value<string>()),
						new KeyValuePair<string, string>("format","json"),
						new KeyValuePair<string, string>("domain",domainname)
					});
					HttpResponseMessage response=_client.PostAsync("Domain.Info",content).Result;
					response.EnsureSuccessStatusCode();
					string json=response.Content.ReadAsStringAsync().Result;
					JObject responseJson=JObject.Parse(json);
					//Console.WriteLine(json);
					if(responseJson["status"]["code"].Value<string>()=="1"){
						return responseJson["domain"].ToObject<JObject>();
					}else{
						throw new System.Exception(responseJson["status"]["message"].Value<string>());
					}
				}
			}));
		}

		private static Task<JObject> BatchUpdateIP(string recordIds,string ip){
			return(Task.Run(()=>{
				using(var _client=new HttpClient(new HttpClientHandler{AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate},false)){
					_client.BaseAddress=new Uri("https://dnsapi.cn/");

					var content=new FormUrlEncodedContent(new[]{
						new KeyValuePair<string, string>("login_token",_config["token"].Value<string>()),
						new KeyValuePair<string, string>("format","json"),
						new KeyValuePair<string, string>("record_id",recordIds),
						new KeyValuePair<string, string>("change","value"),
						new KeyValuePair<string, string>("change_to",ip)
					});
					HttpResponseMessage response=_client.PostAsync("Batch.Record.Modify",content).Result;
					response.EnsureSuccessStatusCode();
					string json=response.Content.ReadAsStringAsync().Result;
					JObject responseJson=JObject.Parse(json);
					//Console.WriteLine(json);
					return(responseJson);
				}
			}));
		}

		private static Task<JArray> findSubDomainInRecordList(string subdomainName,string domainId){
			return(Task.Run(()=>{
				using(var _client=new HttpClient(new HttpClientHandler{AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate},false)){
					_client.BaseAddress=new Uri("https://dnsapi.cn/");

					var content=new FormUrlEncodedContent(new[]{
						new KeyValuePair<string, string>("login_token",_config["token"].Value<string>()),
						new KeyValuePair<string, string>("format","json"),
						new KeyValuePair<string, string>("domain_id",domainId),
						new KeyValuePair<string, string>("sub_domain",subdomainName)
					});
					HttpResponseMessage response=_client.PostAsync("Record.List",content).Result;
					response.EnsureSuccessStatusCode();
					string json=response.Content.ReadAsStringAsync().Result;
					JObject responseJson=JObject.Parse(json);
					//Console.WriteLine(json);
					switch(responseJson["status"]["code"].Value<string>()){
						case "1":
							return responseJson["records"].ToObject<JArray>();
							break;
						case "10":
							return new JArray();
							break;
						default:
							throw new System.Exception(responseJson["status"]["message"].Value<string>());
							break;
					}
				}
			}));
		}
	}
}
