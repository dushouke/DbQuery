# Get Start

 DbQuery is a another mini .net orm,it uses esay and performance is not bad.
 It support C# 3.0+

## ExecuteScalar

    SQlDbQuery.Current.ExecuteScalar<int>(connStr, 
                                      "select id from dbo.Down where id = @id",new { ID = 3220 });

insert a value and return last id

	SQlDbQuery.Current.ExecuteScalar<int>(connStr, 
            "insert into dbo.Down(UserName,CreateTime) values (@Name,@Time);" + SQlDbQuery.LastInsertIDSql,new { Name = "Test", Time = DateTime.Now }); 

## ExecuteQuery

get a simple type list

	 SQlDbQuery.Current.ExecuteQuery<int>(connStr, "select id from dbo.Down where id = @id", 
		                                                              new ExecuteQueryOptional      { Parameters = new { ID = 3220 } });


auto map to a complex type list

    public class DownInfo
    {
        public int SysNo { get; set; }
        public string UserName { get; private set; }
        public DateTime CreateTime { get; set; }
    }
<br/>

	  SQlDbQuery.Current.ExecuteQuery<DownInfo>(connStr, "select * from dbo.Down where id in (@ID)",
			                                                                       new    ExecuteQueryOptional { Parameters = new { ID = new[] { 3220, 3222,3223 } } });


customer mapping

	SQlDbQuery.Current.ExecuteQuery<DownInfo>(connStr, "select * from dbo.Down where id in (@ID)",
	                                                                       new ExecuteQueryOptional
	                                                                       {
	                                                                           Parameters = new { ID = new[] { 3220, 3222, 3223 } },
	                                                                           CustomerMapping = new { SysNo = "ID" }//sysno use the id column
	                                                                       });


manual mapping

	SQlDbQuery.Current.ExecuteQuery<DownInfo>(connStr, "select * from dbo.Down where id in (@ID)",
	                                                                    new ExecuteQueryOptional<DownInfo>
	                                                                    {
	                                                                        Parameters = new { ID = new[] { 3220, 3222, 3223 } },
	                                                                        ReaderFunc = dr => new DownInfo
	                                                                                            {
	                                                                                                SysNo = dr.Get<int>("ID"),
	                                                                                                CreateTime = dr.Get<DateTime>("createtime")
	                                                                                            }
	                                                                    });





