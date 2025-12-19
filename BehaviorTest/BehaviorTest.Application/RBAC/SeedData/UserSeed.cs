using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Tools;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace MyProject.Application.RBAC.SeedData;

public class UserSeed : IEntityTypeBuilder<UserEntity>, IEntitySeedData<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> entityBuilder, DbContext dbContext, Type dbContextLocator)
    {
        entityBuilder.HasKey(u => u.Id);
        entityBuilder.HasIndex(u => u.Name);
        entityBuilder.HasIndex(u => u.Email).IsUnique();
        entityBuilder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        entityBuilder.Property(u => u.Email).IsRequired().HasMaxLength(100);
    }

    public IEnumerable<UserEntity> HasData(DbContext dbContext, Type dbContextLocator)
    {
        return new List<UserEntity>
        {

            new UserEntity{Id=1,Name="张三",Email="363108445@qq.com",Password=DataEncryption.Sha1Encrypt("123456"),Role = "admin"},
            new UserEntity{Id=2,Name="李四",Email="555108445@qq.com",Password=DataEncryption.Sha1Encrypt("123456"),Role = "operator"},
            new UserEntity{Id=3,Name="王五",Email="551238445@qq.com",Password=DataEncryption.Sha1Encrypt("123456"),Role = "viewer"},
        };
    }
}