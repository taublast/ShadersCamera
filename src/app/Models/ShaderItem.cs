namespace ShadersCamera.Models
{
    public class ShaderItem : BindableObject
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string ImageSource { get; set; } = "Images/8.jpg";

        public string ShaderFilename { get; set; }

        /// <summary>
        /// If set will be used instead of filename
        /// </summary>
        public string ShaderCode { get; set; }
    }
}
