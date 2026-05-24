# Logs Sanitizados Runtime SOAP End-to-End Final

Fecha: 2026-05-23 19:00:05 -05:00

```text
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.ChangeTracking[10800]
achinterbank-api  |       DetectChanges starting for 'AchDbContext'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.ChangeTracking[10801]
achinterbank-api  |       DetectChanges completed for 'AchDbContext'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20000]
achinterbank-api  |       Opening connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20001]
achinterbank-api  |       Opened connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20103]
achinterbank-api  |       Creating DbCommand for 'ExecuteReader'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20104]
achinterbank-api  |       Created DbCommand for 'ExecuteReader' (0ms).
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20106]
achinterbank-api  |       Initialized DbCommand for 'ExecuteReader' (0ms).
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20100]
achinterbank-api  |       Executing DbCommand [Parameters=[@p3='?' (DbType = Int64), @p0='?' (DbType = DateTime), @p1='?', @p2='?' (DbType = Boolean)], CommandType='Text', CommandTimeout='30']
achinterbank-api  |       UPDATE "TaskExecutionLog" SET "FinishedAt" = @p0, "Output" = @p1, "Success" = @p2
achinterbank-api  |       WHERE "Id" = @p3;
achinterbank-api  | info: Microsoft.EntityFrameworkCore.Database.Command[20101]
achinterbank-api  |       Executed DbCommand (2ms) [Parameters=[@p1='?' (DbType = Int64), @p0='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
achinterbank-api  |       UPDATE "TaskExecutionLog" SET "FinishedAt" = @p0
achinterbank-api  |       WHERE "Id" = @p1;
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20301]
achinterbank-api  |       Closing data reader to 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20300]
achinterbank-api  |       A data reader for 'ACHInterbank' on server 'tcp://postgres:5432' is being disposed after spending 0ms reading results.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20002]
achinterbank-api  |       Closing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20003]
achinterbank-api  |       Closed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.ChangeTracking[10807]
achinterbank-api  |       An entity of type 'TaskExecutionLog' tracked by 'AchDbContext' changed state from 'Modified' to 'Unchanged'. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see key values.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Update[10005]
achinterbank-api  |       SaveChanges completed for 'AchDbContext' with 1 entities written to the database.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Infrastructure[10407]
achinterbank-api  |       'AchDbContext' disposed.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20007]
achinterbank-api  |       Disposing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20008]
achinterbank-api  |       Disposed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Quartz.Core.JobRunShell[0]
achinterbank-api  |       Trigger instruction : NoInstruction
achinterbank-api  | dbug: Quartz.Core.QuartzSchedulerThread[0]
achinterbank-api  |       Batch acquisition of 0 triggers
achinterbank-api  | info: Microsoft.EntityFrameworkCore.Database.Command[20101]
achinterbank-api  |       Executed DbCommand (5ms) [Parameters=[@p1='?' (DbType = Int64), @p0='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
achinterbank-api  |       UPDATE "TaskExecutionLog" SET "FinishedAt" = @p0
achinterbank-api  |       WHERE "Id" = @p1;
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20301]
achinterbank-api  |       Closing data reader to 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20300]
achinterbank-api  |       A data reader for 'ACHInterbank' on server 'tcp://postgres:5432' is being disposed after spending 0ms reading results.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20002]
achinterbank-api  |       Closing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20003]
achinterbank-api  |       Closed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | info: Microsoft.EntityFrameworkCore.Database.Command[20101]
achinterbank-api  |       Executed DbCommand (4ms) [Parameters=[@p1='?' (DbType = Int64), @p0='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
achinterbank-api  |       UPDATE "TaskExecutionLog" SET "FinishedAt" = @p0
achinterbank-api  |       WHERE "Id" = @p1;
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20301]
achinterbank-api  |       Closing data reader to 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.ChangeTracking[10807]
achinterbank-api  |       An entity of type 'TaskExecutionLog' tracked by 'AchDbContext' changed state from 'Modified' to 'Unchanged'. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see key values.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20300]
achinterbank-api  |       A data reader for 'ACHInterbank' on server 'tcp://postgres:5432' is being disposed after spending 0ms reading results.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20002]
achinterbank-api  |       Closing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20003]
achinterbank-api  |       Closed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Update[10005]
achinterbank-api  |       SaveChanges completed for 'AchDbContext' with 1 entities written to the database.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.ChangeTracking[10807]
achinterbank-api  |       An entity of type 'TaskExecutionLog' tracked by 'AchDbContext' changed state from 'Modified' to 'Unchanged'. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see key values.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Update[10005]
achinterbank-api  |       SaveChanges completed for 'AchDbContext' with 1 entities written to the database.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Infrastructure[10407]
achinterbank-api  |       'AchDbContext' disposed.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20007]
achinterbank-api  |       Disposing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20008]
achinterbank-api  |       Disposed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Infrastructure[10407]
achinterbank-api  |       'AchDbContext' disposed.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20007]
achinterbank-api  |       Disposing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20008]
achinterbank-api  |       Disposed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Quartz.Core.JobRunShell[0]
achinterbank-api  |       Trigger instruction : NoInstruction
achinterbank-api  | dbug: Quartz.Core.JobRunShell[0]
achinterbank-api  |       Trigger instruction : NoInstruction
achinterbank-api  | dbug: Quartz.Core.QuartzSchedulerThread[0]
achinterbank-api  |       Batch acquisition of 0 triggers
achinterbank-api  | info: Microsoft.EntityFrameworkCore.Database.Command[20101]
achinterbank-api  |       Executed DbCommand (5ms) [Parameters=[@p3='?' (DbType = Int64), @p0='?' (DbType = DateTime), @p1='?', @p2='?' (DbType = Boolean)], CommandType='Text', CommandTimeout='30']
achinterbank-api  |       UPDATE "TaskExecutionLog" SET "FinishedAt" = @p0, "Output" = @p1, "Success" = @p2
achinterbank-api  |       WHERE "Id" = @p3;
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20301]
achinterbank-api  |       Closing data reader to 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Command[20300]
achinterbank-api  |       A data reader for 'ACHInterbank' on server 'tcp://postgres:5432' is being disposed after spending 0ms reading results.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20002]
achinterbank-api  |       Closing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20003]
achinterbank-api  |       Closed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.ChangeTracking[10807]
achinterbank-api  |       An entity of type 'TaskExecutionLog' tracked by 'AchDbContext' changed state from 'Modified' to 'Unchanged'. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see key values.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Update[10005]
achinterbank-api  |       SaveChanges completed for 'AchDbContext' with 1 entities written to the database.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Infrastructure[10407]
achinterbank-api  |       'AchDbContext' disposed.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20007]
achinterbank-api  |       Disposing connection to database 'ACHInterbank' on server 'tcp://postgres:5432'.
achinterbank-api  | dbug: Microsoft.EntityFrameworkCore.Database.Connection[20008]
achinterbank-api  |       Disposed connection to database 'ACHInterbank' on server 'tcp://postgres:5432' (0ms).
achinterbank-api  | dbug: Quartz.Core.JobRunShell[0]
achinterbank-api  |       Trigger instruction : NoInstruction
achinterbank-api  | dbug: Quartz.Core.QuartzSchedulerThread[0]
achinterbank-api  |       Batch acquisition of 0 triggers
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:43 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:43 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:43 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "POST /auth/refresh HTTP/1.1" 200 1019 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21253 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17080 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:36:44 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas HTTP/1.1" 200 2123 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /runtime.e1400eec3e845eb1.js HTTP/1.1" 200 3235 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "POST /auth/refresh HTTP/1.1" 200 1019 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:48 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:49 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:49 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21253 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:49 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:37:49 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17080 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:00 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:00 +0000] "GET /integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas HTTP/1.1" 200 2123 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:00 +0000] "GET /runtime.e1400eec3e845eb1.js HTTP/1.1" 200 3235 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:00 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:00 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:00 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "POST /auth/refresh HTTP/1.1" 200 1019 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21237 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:01 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17080 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas HTTP/1.1" 200 2123 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /runtime.e1400eec3e845eb1.js HTTP/1.1" 200 3235 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "POST /auth/refresh HTTP/1.1" 200 1019 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:58 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:59 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21253 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:59 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:39:59 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17095 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas HTTP/1.1" 200 2123 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /runtime.e1400eec3e845eb1.js HTTP/1.1" 200 3235 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:03 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "POST /auth/refresh HTTP/1.1" 200 1019 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21237 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17080 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:41:04 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17095 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas HTTP/1.1" 200 2123 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /runtime.e1400eec3e845eb1.js HTTP/1.1" 200 3235 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "POST /auth/refresh HTTP/1.1" 200 1018 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:31 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:32 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21237 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:32 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:42:32 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17095 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17080 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas HTTP/1.1" 200 2123 "-" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /runtime.e1400eec3e845eb1.js HTTP/1.1" 200 3235 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /polyfills.93a736a453388aa0.js HTTP/1.1" 200 34857 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /styles.be445ec595044621.css HTTP/1.1" 200 197154 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /main.027f6ba598769ebc.js HTTP/1.1" 200 2072315 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /common.467c49deb8f30ec4.js HTTP/1.1" 200 10623 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /179.1943710989da50d1.js HTTP/1.1" 200 112470 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "GET /api/users/branding HTTP/1.1" 200 12 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:42 +0000] "POST /auth/refresh HTTP/1.1" 200 1019 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:43 +0000] "GET /navigation/menu HTTP/1.1" 200 6496 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:43 +0000] "GET /api/integrations/methods HTTP/1.1" 200 1497 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:43 +0000] "POST /api/navigation-logs HTTP/1.1" 200 52 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:43 +0000] "GET /api/integrations/source-catalog?methodId=1 HTTP/1.1" 200 17080 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:43 +0000] "GET /api/integrations/methods/1/parameters HTTP/1.1" 200 9337 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:43:43 +0000] "GET /api/integrations/mappingsets?methodId=1 HTTP/1.1" 200 21237 "http://localhost:743/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/148.0.7778.96 Safari/537.36" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:56:48 +0000] "GET /health/live HTTP/1.1" 200 116 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:56:48 +0000] "GET /health/ready HTTP/1.1" 200 114 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:57:22 +0000] "POST /auth/login HTTP/1.1" 200 1044 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:57:30 +0000] "POST /auth/login HTTP/1.1" 200 1044 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:58:26 +0000] "GET /health/live HTTP/1.1" 200 117 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:58:26 +0000] "GET /health/ready HTTP/1.1" 200 113 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
achinterbank-spa  | 172.18.0.1 - - [23/May/2026:23:58:26 +0000] "POST /auth/login HTTP/1.1" 200 1019 "-" "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.26100.8457" "-"
```

Productivo: NO-GO.
