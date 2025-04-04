using k8s;
using k8s.Models;

namespace CloudflareOperator.Entities;

public static class EntityExtensions
{
    public static V1OwnerReference CreateOwnerReference<T>(this T entity) where T : IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        return new V1OwnerReference
        {
            ApiVersion = entity.ApiVersion,
            Kind = entity.Kind,
            Name = entity.Name(),
            Uid = entity.Uid()
        };
    }
}