using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Tests
{
    class TestSaveInterceptor : SaveInterceptor<InterceptedAndFiltered>
    {
        public override void Apply(InterceptedAndFiltered entity)
        {
            entity.Test = !entity.Test;
        }
    }


    [TestClass]
    public class TestInterceptors
    {
        [TestMethod]
        public async Task should_register_interceptor()
        {
            var dbContext = new DbContext();
            dbContext.RegisterSaveInterceptors(new TestSaveInterceptor());
            Assert.IsTrue(dbContext.SaveInterceptors.Any(x => x is TestSaveInterceptor));
            Assert.IsTrue(DB.SaveInterceptors.Any(x => x.Value is TestSaveInterceptor));
            //Assert.IsTrue(dbContext.SaveInterceptors.Any(x => x is TestSaveInterceptor));
            //Assert.IsTrue(DB.SaveInterceptors.Any(x => x is TestSaveInterceptor));
        }

        [TestMethod]
        public async Task save_interceptors_should_work()
        {
            var dbContext = new DbContext();
            dbContext.RegisterSaveInterceptors(new TestSaveInterceptor());
            await dbContext.DeleteAsync<InterceptedAndFiltered>(x => true);
            var testEntity = new InterceptedAndFiltered()
            {
                Test = true
            };
            dbContext.AttachContextSession(testEntity);
            await testEntity.SaveAsync();
            Assert.IsTrue(!testEntity.Test);
            await dbContext.CommitAsync();
            dbContext = new DbContext();
            testEntity = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            Assert.IsTrue(!testEntity.Test);
        }
    }
}
