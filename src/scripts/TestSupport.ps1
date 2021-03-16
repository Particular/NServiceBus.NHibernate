function Cleanup ()
{
	sqlcmd -S .\SQLEXPRESS -d NServiceBus -i .\Reset-Database.sql | Out-Null
}