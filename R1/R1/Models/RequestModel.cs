using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.IO;

namespace R1.Models
{
    //request
    public class RequestModel
    {
        //[DefaultValue("deepseek-r1:32b")]
        public string model { get; set; } = "deepseek-r1:32b";

        //[DefaultValue(false)]
        public bool stream { get; set; } = false;

        //[DefaultValue(600)]
        public int keep_alive { get; set; } = 600;

        [Required]
        public string prompt { get; set; }
    }


    public class Embend
    {
        public string model { get; set; } = "deepseek-r1:32b";
        public string input { get; set; }
    }


    public class EmbendResponse
    {
        public string model { get; set; }
        public List<List<double>> embeddings { get; set; }
        public long total_duration { get; set; }
        public long load_duration { get; set; }
        public int prompt_eval_count { get; set; }
    }
}