using ManwhaWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class ProfilePictureController : Controller
    {
        private readonly BlobStorageService _blob;

        public ProfilePictureController(BlobStorageService blob)
        {
            _blob = blob;
        }

        [HttpGet("/profile-picture/{blobName}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult Get(string blobName)
        {
            var sasUrl = _blob.GetSasUrl(blobName, TimeSpan.FromHours(1));
            return Redirect(sasUrl);
        }
    }
}
