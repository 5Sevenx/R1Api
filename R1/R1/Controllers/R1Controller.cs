using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using R1.Models;

namespace cashreg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class R1Constoller : ControllerBase
    {
        private readonly HttpClient _httpClient;

        private readonly IDistributedCache _cache;


        public R1Constoller(HttpClient httpClient, IDistributedCache cache)
        {
            _httpClient = httpClient;
            _cache = cache; 
        }

        [HttpPost]
        public async Task<ActionResult<RequestModel>> Request([FromBody] RequestModelDTO R1)
        {
            var json = JsonSerializer.Serialize(new
            {
                R1.model,
                R1.stream,
                R1.keep_alive,
                R1.prompt
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://ia.simaladserver.es/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var responseBody = await response.Content.ReadAsStringAsync();
       
            //parse json for get only on field
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                string getresponse = doc.RootElement.GetProperty("response").GetString();

                return Ok(getresponse);
            }
            
        }

        [HttpPost("htmltotext")]
        public async Task<ActionResult<EmbendResponse>> FromHtmlToText()
        {
            var response = await _httpClient.GetAsync("https://irismedia.es/wp-json/wp/v2/posts/");

            //validation
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            using var document  = JsonDocument.Parse(json);

            //save result
            var results = new List<string>();

           //save vector
            var vectors = new List<double[]>();


            //save simlarity results
            var similarity = new List<List<double>>();

            //list per each element 
            foreach (var post in document.RootElement.EnumerateArray())
            {
                //get content
                var contentHtml = post.GetProperty("content").GetProperty("rendered").GetString();

                //parse to txt
                var rawText = Regex.Replace(contentHtml, "<.*?>", "").Trim();
                var plaintext = WebUtility.HtmlDecode(rawText);

                //parse to json
                var embendPayload = new Embend { model = "deepseek-r1:32b", input = plaintext };

                var requestmodel = JsonSerializer.Serialize(embendPayload);
                //post
                var postcontent = new StringContent(requestmodel, Encoding.UTF8, "application/json");

                //response 
                var postresponse = await _httpClient.PostAsync("https://ia.simaladserver.es/api/embed", postcontent);

          
                if(postresponse.IsSuccessStatusCode)
                {
                    //5119 == 5120 final embendings,each
                    var resultJson = await postresponse.Content.ReadAsStringAsync();
                  
                    results.Add(resultJson);

                    var embendResponse = JsonSerializer.Deserialize<EmbendResponse>(resultJson);

                    vectors.Add(embendResponse.embeddings[0].ToArray());
                }
            }

            for(int i = 0; i < vectors.Count; i++) {

                //list for each pair 
                var pairSimilarity = new List<double>();

                //set j+1 just to not compare the same vector
                for(int j = i + 1; j < vectors.Count; j++)
                {
                    //set vectors to compare with a 
                    double[] a = vectors[i];
                    double[] b = vectors[j];

                    //scale 
                    double scale = a.Zip(b,(x,y) => x*y).Sum();

                    //vectors
                    double nomrA = Math.Sqrt(a.Sum(x => x *x));
                    double normB = Math.Sqrt(b.Sum(x => x * x));

                    //sum vectors
                    double vectorsum = nomrA * normB;

                    //get similarity
                    double _similarity = scale / vectorsum;

                    //add it to pair list
                    pairSimilarity.Add(_similarity);
                }
    
                //add each pair list to not be confused 
                similarity.Add(pairSimilarity);
            }

            return Ok(new { results, similarity });
        }
    }
}