using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestFilters
    {
        [TestMethod]
        public async Task should_register_data_filter()
        {
            var dbContext = new DbContext();
            dbContext.RegisterDataFilters(new DelegateDataFilter<InterceptedAndFiltered>(x => true));
            Assert.IsTrue(dbContext.DataFilters.Any(x => x is DelegateDataFilter<InterceptedAndFiltered>));
            Assert.IsTrue(DB.DataFilters.Any(x => x.Value is DelegateDataFilter<InterceptedAndFiltered>));
            //Assert.IsTrue(dbContext.SaveInterceptors.Any(x => x is TestSaveInterceptor));
            //Assert.IsTrue(DB.SaveInterceptors.Any(x => x is TestSaveInterceptor));
        }

        [TestMethod]
        public async Task query_data_filters_should_work()
        {
            var getValue = new Func<bool>(() =>
            {
                return false;
            });
            var dbContext = new DbContext();
            dbContext.RegisterDataFilters(new DelegateDataFilter<InterceptedAndFiltered>(x => x.Test == getValue()));
            await dbContext.DeleteAsync<InterceptedAndFiltered>(x => true);
            var testEntity = new InterceptedAndFiltered()
            {
                Test = true
            };
            dbContext.AttachContextSession(testEntity);
            await testEntity.SaveAsync();
            await dbContext.CommitAsync();
            dbContext = new DbContext();
            var result = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            Assert.IsNull(result);
            var resultList = await dbContext.Find<InterceptedAndFiltered>().ExecuteAsync();
            Assert.AreEqual(0, resultList.Count);
        }
    }
}