using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using R1.Models;

namespace cashreg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class R1Constoller : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public R1Constoller(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<ActionResult<RequestModel>> Request([FromBody] RequestModel R1)
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
            return Ok(responseBody);
        }

        [HttpPost("htmltotext")]
        public async Task<ActionResult> FromHtmlToText()
        {
            var response = await _httpClient.GetAsync("https://irismedia.es/wp-json/wp/v2/posts/");

            //validation
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            using var document  = JsonDocument.Parse(json);

            //save result
            var results = new List<object>();

            var vectors = new List<double[]>();

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
                    //5119 embendings each
                    var resultJson = await postresponse.Content.ReadAsStringAsync();
                  
                    results.Add(resultJson);

                    var embendResponse = JsonSerializer.Deserialize<EmbendResponse>(resultJson);

         
                        vectors.Add(embendResponse.embeddings[0].ToArray());

                }
            }
            return Ok(results);
        }
    }
}