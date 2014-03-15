using System;
using System.Collections.Generic;
using System.Text;
using HUtils.DBTasks;
using System.Data;

namespace DBUtils.Test
{
    public enum eCItyType : byte
    {
        Unknown = 0,
        Village = 1,
        Town = 2
    }
    public class CityContract
    {
        public int CityID { get; set; }
        public string Name { get; set; }
        public eCItyType Type { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DestroyDate { get; set; }
        public int RegionID { get; set; }
        public string RegionName { get; set; }
        public DateTime RegionCreateDate { get; set; }
        public DateTime RegionDestroyDate { get; set; }
    }
    public class SimpleCityContract
    {
        public int CityID { get; set; }
        public string Name { get; set; }
        public eCItyType Type { get; set; }
        public int RegionID { get; set; }
    }
    public class TestContract
    {
        public int ID { get; set; }
        public string Value { get; set; }
    }

    public class LibTask : BaseDBTask
    {
        public static IEnumerable<T> GetCities<T>(object parameters)
        {
            return GetSingleListResult<T>(parameters);
        }
    }

    public class MyDBCommandAttribute : DBCommandAttribute
    {
        public string add { get; set; }
        protected override string GetCommandText()
        {
            var st = base.GetCommandText();
            return st + add;
        }
    }

    [ConnectionStringName("default1")]
    public class MyTask : BaseDBTask
    {
        public static IEnumerable<T> MyMethod<T>(object parameters)
        {
            return GetSingleListResult<T>(parameters);
        }

        [DBCommand(CommandText = "testtasktestmethodretval", CommandType = CommandType.StoredProcedure)]
        public static int returnval(object parameters)
        {
            return GetReturnValue(parameters);
        }

        [DBCommand(CommandText = "select * from test4 where val = @val", CommandType = CommandType.Text)]
        //[MyDBCommandAttribute(CommandText = "select * from test4 ", add = "where val = @val", CommandType = CommandType.Text)]        
        public static IEnumerable<T> GetValues<T>(object parameters)
        {
            return GetSingleListResult<T>(parameters);
        }
    }
    public class Test4Res
    {
        public int ID { get; set; }
        public string Val { get; set; }
    }

    public class IDTable
    {
        public int ID { get; set; }
        public string Value { get; set; }
    }
    public class TestInParam
    {
        public IDTable[] ids { get; set; }
        public string Value { get; set; }
    }


    public class WaterPointContract
    {
        public int WaterPointID { get; set; }
        public int ContractID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DestroyDate { get; set; }
        public bool HasNoDrainage { get; set; }
        public bool IsIrrigation { get; set; }
        public string Memo { get; set; }
        public byte NotSupplyDays { get; set; }
        public int SupplyNodeID { get; set; }
        public string StampNumber { get; set; }
        public decimal SupplyTime { get; set; }
        public decimal Tariff { get; set; }
        public decimal TubeDiameter { get; set; }
        public int SupplySchedule1ID { get; set; }
        public int SupplySchedule2ID { get; set; }

        #region Aditional Members
        public IEnumerable<CounterContract> Counters { get; set; }

        #endregion
    }
    public class DrainagePointContract
    {
        public int DrainagePointID { get; set; }
        public int ContractID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DestroyDate { get; set; }
        public byte PointsCount { get; set; }
        public decimal Tariff { get; set; }

        #region Aditional Members
        public IEnumerable<CounterContract> Counters { get; set; }

        #endregion
    }
    public class CounterContract
    {
        public int CounterID { get; set; }
        public string Type { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DestroyDate { get; set; }
        public DateTime? LastCheckDate { get; set; }
        public string LastCheckNumber { get; set; }
        public short LaunchYear { get; set; }
        public string Number { get; set; }
        public int PointID { get; set; }
        public int SignsNumber { get; set; }
        public string StampNumber { get; set; }
    }
    class ContractGetPointsResult
    {
        public WaterPointContract[] WaterPoints { get; set; }
        public IEnumerable<CounterContract> WaterPointCounters { get; set; }
        public IEnumerable<DrainagePointContract> DrainagePoints { get; set; }
        public IEnumerable<CounterContract> DrainagePointCounters { get; set; }
    }
    public sealed class ResidentContractTask : BaseDBTask
    {
        public static T GetPoints<T>(int userId, int residentContractID)
        {
            return GetMultipleListResult<T>(new { UserId = userId, ResidentContractID = residentContractID });
        }
    }

    class User
    {
        public long UserID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public byte Status { get; set; }
    }

    [ConnectionStringName(ConnectionStringName="adsrialto")]
    class UsersTask : BaseDBTask
    {
        public static void Save(User user)
        {
            var userID = GetReturnValue(user);
            user.UserID = (long)userID;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //var city = LibTask.GetCities<CityContract>(new { CityID = 16777218});
            //var cities = LibTask.GetCities<CityContract>(null);
            //var inParam = new TestInParam { ids = new IDTable[] { new IDTable { ID = 1, Value = "a" }, new IDTable { ID = 2, Value = "b" } }, Value = "f" };
            //var testcontracts = MyTask.MyMethod<TestContract>(inParam);


            //var points = ResidentContractTask.GetPoints<ContractGetPointsResult>(2, 83886982);

            try
            {

                var retval = MyTask.returnval(new { Value = "asdsad" });
            }
            catch (Exception ex)
            {
            }

            //var res4a = MyTask.GetValues<Test4Res>(new { val = "a" });
            //var res4c = MyTask.GetValues<Test4Res>(new { val = "c" });

            var user = new User
            {
                UserID = 9,
                Email = "asdsfhhfjjghad",
                Password = "23ghjghjhg"
            };

            //UsersTask.Save(user);

        }
    }
}
