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






##Performance


<iframe width="485" height="318" frameborder="0" scrolling="no" src="https://r.office.microsoft.com/r/rlidExcelEmbed?su=3435790709610489208&Fi=SD2FAE617260EBA578!233&ak=t%3d0%26s%3d0%26v%3d!ACuhxuTLasy-Kp0&kip=1&wdAllowInteractivity=False&Item=Chart%201&wdDownloadButton=True"></iframe>




