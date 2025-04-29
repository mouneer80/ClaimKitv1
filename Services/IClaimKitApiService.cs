using System.Threading.Tasks;
using ClaimKitv1.Models.Responses;
using ClaimKitv1.Models;
using Newtonsoft.Json.Linq;

namespace ClaimKitv1.Services
{
    public interface IClaimKitApiService
    {
        Task<ReviewResponse> ReviewNotesAsync(ReviewRequest request);
        Task<EnhanceResponse> EnhanceNotesAsync(EnhanceRequest request);
        Task<GenerateClaimResponse> GenerateClaimAsync(GenerateClaimRequest request);
    }
}