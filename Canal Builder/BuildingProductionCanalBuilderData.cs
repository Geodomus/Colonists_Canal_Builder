using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Canal_Builder
{
    public class BuildingProductionCanalBuilderData : BuildingProductionData
    {
        private readonly List<FloorTileLayer> _selectedRouteLayers = new List<FloorTileLayer>();
        private List<FloorTileLayerRef> _ppSelectedRouteLayerFloorPositions = new List<FloorTileLayerRef>();

        private FloorTileLayer _currentWorkerTargetLayer;
        private FloorTileLayerRef _ppCurrentWorkerTargetLayerRef;
        private Vector3 _nextSubPosition;
        private List<FloorTileLayer> _neighboursNotInPrio = new List<FloorTileLayer>();
        public BuildingProductionCanalBuilderData() => this.typeId = (TypeID)201;

        public override BuildingProductionDataInfo GetInfo() => this.building.GetInfo()
            .GetAgeCompInfo<BuildingProductionCanalBuilderDataInfo>(this.building.age,
                (TypeID)201);

        public override BuildingProductionDataInfo GetUpgradeInfo() => this.building
            .GetInfo().GetAgeCompInfo<BuildingProductionCanalBuilderDataInfo>(this.building.age.ToNextAge(),
                (TypeID)201);

        public override void OnCreation(Entity cEntity)
        {
            base.OnCreation(cEntity);
        }

        public override void OnPostDeserialise(Entity dEntity)
        {
            base.OnPostDeserialise(dEntity);
            this._selectedRouteLayers.Clear();
            foreach (var t in this._ppSelectedRouteLayerFloorPositions)
                this._selectedRouteLayers.Add(
                    Game.instance.floorData.GetFloorTileLayerByRef(t));

            if (this._ppCurrentWorkerTargetLayerRef != null)
                this._currentWorkerTargetLayer = this._ppCurrentWorkerTargetLayerRef.GetLayer();
        }

        public override void OnDestroy()
        {
            this.OnLose();
            base.OnDestroy();
        }

        public override void OnLose()
        {
            base.OnLose();
            if (!(this._currentWorkerTargetLayer != null))
                return;
            this._currentWorkerTargetLayer.Unreserve();
            this._currentWorkerTargetLayer = null;
        }
        
        /*protected override bool TryWorkerUnitHeadToProduction(Unit unit)
        {
            this._selectedRouteLayers.Clear();
            if (this.building.priorityLayersData.priorityLayers.Count > 0)
            {
                int count = this.building.priorityLayersData.priorityLayers.Count;
                while (count-- > 0)
                {
                    FloorTileLayer priorityLayer = this.building.priorityLayersData.priorityLayers[count];
                    if (priorityLayer.IsUnoccupied() && priorityLayer.parentTile.owner == this.building.ownerData.owner && !priorityLayer.IsBelowWater())
                    {
                        FloorTileLayerPathfinder.CalculateFloorTileLayerRoute(this._selectedRouteLayers,
                            this.building.occupierData.mainBaseLayer,
                            priorityLayer);
                        if (this._selectedRouteLayers.Count > 0)
                        {
                            this._currentWorkerTargetLayer = priorityLayer;
                            break;
                        }
                    }
                }
            }
            if (this._selectedRouteLayers.Count <= 0)
                return false;
            this._selectedRouteLayers[this._selectedRouteLayers.Count - 1]
                    .ReserveForFutureOccupier(EntityInfo.Type.STRUCTURE, 201, unit);
            return true;
        }*/

        protected override bool TryWorkerUnitHeadToProduction(Unit unit)
        {
            this._selectedRouteLayers.Clear();
            if (this.building.priorityLayersData.priorityLayers.Count > 0 && this._selectedRouteLayers.Count == 0)
            {
                int count = this.building.priorityLayersData.priorityLayers.Count;
                while (count-- > 0)
                {
                    FloorTileLayer priorityLayer = this.building.priorityLayersData.priorityLayers[count];
                    this._neighboursNotInPrio.AddRange(GetNeighbours(priorityLayer));
                    
                    if (priorityLayer.IsUnoccupied() && priorityLayer.parentTile.owner == this.building.ownerData.owner && !priorityLayer.IsBelowWater())
                    {
                        FloorTileLayerPathfinder.CalculateFloorTileLayerRoute(this._selectedRouteLayers,
                            this.building.occupierData.mainBaseLayer,
                            priorityLayer);
                        if (this._selectedRouteLayers.Count > 0)
                        {
                            Debug.Log("prio " + count);
                            this._currentWorkerTargetLayer = priorityLayer;
                            break;
                        }
                    }
                }
            }

            if (_neighboursNotInPrio.Count > 0 && this._selectedRouteLayers.Count == 0)
            {
                List<FloorTileLayer> neighbors = new HashSet<FloorTileLayer>(_neighboursNotInPrio).ToList();
                for (int count = 0; count < neighbors.Count; count++)
                {
                    FloorTileLayer neighbourLayer = neighbors[count];
                    Debug.Log(neighbourLayer);
                    if (neighbourLayer != null && neighbourLayer.IsUnoccupied() && neighbourLayer.parentTile.owner == this.building.ownerData.owner && neighbourLayer.terrainCategory1 != TerrainCategory.Rock && neighbourLayer.terrainCategory2 != TerrainCategory.Rock)
                    {
                        FloorTileLayerPathfinder.CalculateFloorTileLayerRoute(this._selectedRouteLayers,
                            this.building.occupierData.mainBaseLayer,
                            neighbourLayer);
                        if (this._selectedRouteLayers.Count > 0)
                        {
                            this._currentWorkerTargetLayer = neighbourLayer;
                            break;
                        }
                    }
                }
            }
            if (this._selectedRouteLayers.Count <= 0)
                return false;
            this._selectedRouteLayers[this._selectedRouteLayers.Count - 1]
                    .ReserveForFutureOccupier(EntityInfo.Type.STRUCTURE, 201, unit);
            return true;
        }

        protected override void SendUnitToWork(Unit unit)
        {
            FloorTileLayerSubLocation layerSubLocation = ReusableData.GetLayerSubLocation();
            layerSubLocation.layer = this._selectedRouteLayers[this._selectedRouteLayers.Count - 1];
            unit.HeadToWork(this._selectedRouteLayers, layerSubLocation);
            this._selectedRouteLayers.Clear();
        }

        public override void OnCompleteWorkerProcessing(Unit unit)
        {
            Game.instance.AddCustomLog(new DebugGameCustomLogCommand.Entry()
            {
                type = DebugGameCustomLogCommand.EntryType.ForestrCompleteWorking,
                uid = this.building.uid
            });
            if (unit.locomotorData.currentLayer.IsUnoccupied() && unit.locomotorData.currentLayer.parentTile.owner == this.building.ownerData.owner && unit.locomotorData.currentLayer.IsReservedBy(unit))
            {
                if (_neighboursNotInPrio.Contains(unit.locomotorData.currentLayer))
                {
                    unit.locomotorData.currentLayer.SetTerrain(TerrainId.Rock);
                    _neighboursNotInPrio.Remove(unit.locomotorData.currentLayer);
                }
                else
                {
                    unit.locomotorData.currentLayer.SetTileHeight(70);
                }
            }

            if (this._currentWorkerTargetLayer != null)
            {
                this._currentWorkerTargetLayer.Unreserve();
                this._currentWorkerTargetLayer = null;
            }
            this.OnCompleteBuildingProcessing();
            unit.ReturnToIdle(Unit.State.WORKER_FROM_PROCESSING);
        }

        protected override void CancelProduction()
        {
            base.CancelProduction();
            if (!(this._currentWorkerTargetLayer != null))
                return;
            this._currentWorkerTargetLayer.Unreserve();
            this._currentWorkerTargetLayer = null;
        }

        public override int Serialise(byte[] buffer, int offset)
        {
            List<FloorTileLayerRef> objects2 = new List<FloorTileLayerRef>();
            int count2 = this._selectedRouteLayers.Count;
            for (int index = 0; index < count2; ++index)
            {
                var layerRef = new FloorTileLayerRef(_selectedRouteLayers[index].coord, _selectedRouteLayers[index].id);
                objects2.Add(layerRef);
            }
            offset = DataEncoder.EncodeList(objects2, buffer, offset);
            offset = DataEncoder.EncodeFloorTileLayer(this._currentWorkerTargetLayer, buffer, offset);
            offset = DataEncoder.Encode(this._nextSubPosition, buffer, offset);
            return base.Serialise(buffer, offset);
        }

        public override int Deserialise(byte[] buffer, int offset)
        {
            this._ppSelectedRouteLayerFloorPositions = new List<FloorTileLayerRef>();
            offset = DataEncoder.DecodeList(out this._ppSelectedRouteLayerFloorPositions, buffer, offset);
            offset = DataEncoder.DecodeFloorTileLayer(out this._ppCurrentWorkerTargetLayerRef, buffer, offset);
            offset = DataEncoder.Decode(out this._nextSubPosition, buffer, offset);
            return base.Deserialise(buffer, offset);
        }

        public override bool IsValidPriorityLayer(FloorTileLayer layer)
        {
            return layer.IsUnoccupied() && !layer.IsBelowWater() &&
                   layer.parentTile.owner == this.building.ownerData.owner && !layer.HasAnyLandRoutes();
        }

        private List<FloorTileLayer> GetNeighbours(FloorTileLayer layer)
        {
            List<FloorTileLayer> neighbours = new List<FloorTileLayer>();
            foreach (FloorIntVector2 vector in FloorIntVector2.allDirections)
            {
                if (!this.building.priorityLayersData.priorityLayers.Contains(layer.GetNeighbour(vector)))
                {
                    if(layer.GetNeighbour(vector) != null)
                        if(!layer.GetNeighbour(vector).IsBelowWater())
                            if(!neighbours.Contains(layer.GetNeighbour(vector)))
                                neighbours.Add(layer.GetNeighbour(vector));
                }
            }
            return neighbours;
        }
    }
}