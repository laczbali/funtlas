using OpenStreetMap.Data.Models;

namespace OpenStreetMap.Data
{
    internal static class ViewDefinitions
    {
        internal const string CreateViewBaseQuery = $@"CREATE VIEW ? AS";

        internal const string CompoundWayData = $@"
            SELECT
	            CW.{nameof(CompoundWay.Id)} as Id,
	            count(*) as NodeCount,
	            avg(W.{nameof(Way.AverageAngle)}) as AvgAngle,
	            sum(WN.{nameof(WayNode.HasCrossing)}) as CrossingCount
            FROM
	            {CompoundWay.TableName} as CW
            LEFT JOIN {CompoundWayPart.TableName} as CWP on CWP.{nameof(CompoundWayPart.CompoundWayId)} = CW.{nameof(CompoundWay.Id)}
            LEFT JOIN {Way.TableName} as W on W.{nameof(Way.Id)} = CWP.{nameof(CompoundWayPart.WayId)}
            LEFT JOIN {WayNode.TableName} as WN on WN.{nameof(WayNode.WayId)} = W.{nameof(Way.Id)}
            LEFT JOIN {Node.TableName} as N on N.{nameof(Node.Id)} = WN.{nameof(WayNode.NodeId)}
            GROUP BY CW.{nameof(CompoundWay.Id)}
        ";
    }
}
