using System.Reflection;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Application.Common.Extension;

// public class SieveProcessorExtension : SieveProcessor
// {
//     public SieveProcessorExtension(IOptions<SieveOptions> options, EntitySieveCustomSortMethods sortMethods, EntitySieveCustomFilterMethods filterMethods) 
//         : base(options, sortMethods, filterMethods)
//     {
//     }
//
//     protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
//     {
//         return mapper.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
//     }
// }