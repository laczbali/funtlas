using OpenStreetMap.Data.Models;

namespace OpenStreetMap.Data
{
    internal static class SqlQueries
    {
        internal const string GetAllNodes = @$"
            SELECT *
            FROM {Node.TableName}
        ";

        internal const string GetAllWays = @$"
            SELECT *
            FROM {Way.TableName}
        ";

        internal const string GetAllWayNodes = @$"
            SELECT *
            FROM {WayNode.TableName}
        ";

        internal const string GetAllNodesOfWay = @$"
            SELECT
	            N.*
            FROM {Node.TableName} as N
            INNER JOIN {WayNode.TableName} as WN ON
	            WN.{nameof(WayNode.NodeId)} = N.Id
	            and WN.{nameof(WayNode.WayId)} = ?
            ORDER BY WN.SortOrder
        ";

        internal const string GetAllWayNodesOfNode = $@"
            SELECT
	            *
            FROM
	            {WayNode.TableName}
            WHERE
	            {nameof(WayNode.NodeId)} = ?
        ";

        internal const string GetNonCrossEndWayNodes = @$"
            SELECT
	            WN.*
            FROM
	            {WayNode.TableName} as WN
            WHERE
	            WN.{nameof(WayNode.IsEndNode)} = 1
                and WN.{nameof(WayNode.IsCrossroad)} = 0
        ";
    }
}
