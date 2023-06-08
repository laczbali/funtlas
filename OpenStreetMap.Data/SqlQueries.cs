using OpenStreetMap.Data.Models;

namespace OpenStreetMap.Data
{
    internal static class SqlQueries
    {
        internal const string GetAllNodes = @$"
            SELECT *
            FROM {Node.TableName}
        ";

        internal const string GetAllWayNodes = @$"
            SELECT *
            FROM {WayNode.TableName}
        ";
    }
}
