using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class MazeManager : MonoBehaviour
{
    public Text txt;

    public Sprite sprite;
    public int width = 2, height = 2;
    public Cell[,] grid;
    private int viewportX, viewportY;
    private List<System.Action> actions = new List<System.Action>();
    private int selectedAction = 0;
    private int[] sizes = new int[] {4,8,16,32,64};
    public string longestPath;

    // Start is called before the first frame update
    void Start()
    {
        /* ### ALGORITHMS ### */
        this.actions.Add( () => this.BinaryTreeAlgorithm());
        this.actions.Add( () => this.SideWinderAlgorithm());
        this.actions.Add( () => this.AldousBroderAlgoritm());
        this.actions.Add( () => this.HuntAndKill());
        this.actions.Add( () => this.Wilson());
        this.actions.Add( () => this.RecursiveBacktracker());

        var l = this.gameObject.AddComponent<LineRenderer>();
        this.width = 4;
        this.height = 4;
        Init();
        this.CallAlgorithm(0);
        DrawMaze();


    }


    void DrawMaze() {
        /* ### DRAW MAZE ### */
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                this.grid[x, y].DrawTile();
            }
        }
    }

    void ResetMaze() {
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                this.grid[x, y].ResetTile();
            }
        }
    }

    void Init() {
        this.viewportY = (int) Camera.main.orthographicSize;
        this.viewportX = (int) viewportY * (Screen.width / Screen.height);
        this.grid = new Cell[this.width, this.height];
       
        /* ### SETUP ### */
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                this.grid[x, y] = new Cell(x, y, this);
            }
        }
        var l = this.gameObject.GetComponent<LineRenderer>();
        l.startWidth = 0.1f;
        l.positionCount = 5;
        l.SetPosition(0, this.grid[0, 0].SouthWestPoint());
        l.SetPosition(1, this.grid[0, this.height - 1].NorthWestPoint());
        l.SetPosition(2, this.grid[this.width - 1, this.height - 1].NorthEastPoint());
        l.SetPosition(3, this.grid[this.width - 1, 0].SouthEastPoint());
        l.SetPosition(4, this.grid[0, 0].SouthWestPoint());
        
    }

    void CallAlgorithm(int index) {
        
        var algorithm = this.actions[index];
        algorithm.Invoke();

        int longestPath = this.DijkstraLongestPath();
        Debug.Log("Longest path: " + longestPath);
        var newString = "Longest path: " + longestPath;
        this.txt.text = newString;
    }

    // Update is called once per frame
    void Update()
    {
        // intentionally left blank 
    }

    public void HandleInput(int val) {
        this.selectedAction = val;
        ResetMaze();
        Init();
        CallAlgorithm(val);
        DrawMaze();
    }

    public void HandleResize(int val) {
        var size = this.sizes[val];
        if (size != this.width){
            for(int x = 0; x < this.width; x++)
            {
                for(int y = 0; y < this.height; y++)
                {
                    this.grid[x, y].DestroyTile();
                    
                }
            }
            this.width = size;
            this.height = size;
            Init();
            CallAlgorithm(this.selectedAction);
            DrawMaze();
        }
    }

    public void BinaryTreeAlgorithm()
    {
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                bool r = (Random.value > 0.5f);
                Cell c = this.grid[x, y];
                if (c.east != null && c.north != null) 
                {
                    c.east.isVisible = r;
                    c.north.isVisible = !r;
                } 
                else if (c.east != null)
                {
                    c.east.isVisible = false;
                }
                else if (c.north != null)
                {
                    c.north.isVisible = false;
                }
                else 
                {
                    // top right corner, so do nothing
                }
            }
        }
    }

    public void SideWinderAlgorithm()
    {
        int firstRow = this.height - 1;
        for(int row = this.height - 1; row >= 0; row--){
            // No east walls on north row
            if (row == firstRow) {
                for(int col=0; col<this.width; col++) {
                    CarveOutEastWall(this.grid[col,row]);
                }
            }
            List<Cell> run = new List<Cell>();
            for(int col = 0; col < this.width; col++) {
                int toExtend = Random.Range(0, 2); // 0 or 1
                if (toExtend == 1)
                {
                    run.Add(this.grid[col,row]);
                }
                else 
                {
                    run.Add(this.grid[col,row]);
                    DrawRun(run);
                    run = new List<Cell>();
                }
            }
            DrawRun(run);
        }
    }

    public void AldousBroderAlgoritm()
    {
        Cell c = this.PickRandomCell();
        while (!this.AllCellsAreVisited()) 
        {
            if (!c.isVisited)
            {
                c.isVisited = true;
            }
            c = this.MoveToRandomAdjacentCell(c);
        }
    }

    public void RecursiveBacktracker()
    {
        Cell c = this.PickRandomCell();
        c.isVisited = true;
        this.Backtracker(c);
    }

    public void Backtracker(Cell c)
    {
        var cells = this.GetNonVisitedNeighbours(c);
        while(cells.Count != 0) 
        {
            var direction = (new List<string>(cells.Keys))[Random.Range(0, cells.Keys.Count)];
            var cell = cells[direction];
            cell.isVisited = true;
            this.CarveOutDirection(c, direction);

            this.Backtracker(cell);
            cells = this.GetNonVisitedNeighbours(c);
        }
    }
    
    public void HuntAndKill()
    {
        Cell c = this.PickRandomCell();
        c.isVisited = true;
        List<Cell> cells = new List<Cell>();
        cells.Add(c);
        while(!this.AllCellsAreVisited()) 
        {
            cells.AddRange(this.RandomWalk(c));
            foreach (Cell cell in cells) 
            {
                if(this.GetNonVisitedNeighbours(cell).Count != 0)
                {
                    c = cell;
                    break;
                }
            }
        }
    }

    public List<Cell> RandomWalk(Cell c)
    {
        List<Cell> walk = new List<Cell>();
        var cells = this.GetNonVisitedNeighbours(c);
        while(cells.Count != 0)
        {
            var direction = (new List<string>(cells.Keys))[Random.Range(0, cells.Keys.Count)];
            var cell = cells[direction];
            walk.Add(cell);
            this.CarveOutDirection(c, direction);
            cell.isVisited = true;
            cells = this.GetNonVisitedNeighbours(cell);
            c = cell;
        }

        return walk;
    }

    public void Wilson() {
        Cell c = this.PickRandomCell();
        var cells = new List<Cell>();
        c.isVisited = true;
        cells.Add(c);
        Cell chosenCell;
        while (!this.AllCellsAreVisited()) {
            // perform walk from chosen to maze
            chosenCell = this.PickRandomUnvisitedCell();
            cells.AddRange(this.RandomWalkWithRevisit(chosenCell, cells));
        }
    }

    public List<Cell> RandomWalkWithRevisit(Cell c, List<Cell> maze) {
        List<Cell> walk = new List<Cell>();
        c.isVisited = true;
        walk.Add(c);
        var cells = this.GetNeighbours(c);
        var cell = cells[Random.Range(0, cells.Count)];
        while (!maze.Contains(cell)) {
            walk.Add(cell);
            cells = this.GetNeighbours(cell);
            cell = cells[Random.Range(0, cells.Count)];
        }
        // remove loops
        var copy = new List<Cell>(walk);
        foreach (Cell _c in copy) {
            if (copy.Count(item => item == _c) > 1) {
                //snij alles tussen de 1e en laatste occurence weg
                var firstIndex = walk.FindIndex(x => x == _c);
                var lastIndex = walk.LastIndexOf(_c);
                if (firstIndex != lastIndex) {
                    walk.RemoveRange(firstIndex, (lastIndex - firstIndex));
                }
            }
        }
        for (int i = 0; i < walk.Count - 1; i++) {
            var currentCell = walk[i];
            var nextCell = walk[i+1];
            nextCell.isVisited = true;
            this.CarveWallBetween(currentCell, nextCell);
        }
        this.CarveWallBetween(walk[walk.Count - 1], cell);

        return walk;
    }

    
    public List<Cell> GetNeighbours(Cell c)
    {
        List<Cell> cells = new List<Cell>();
        Cell n = this.GetNorthernCell(c);
        Cell e = this.GetEasternCell(c);
        Cell s = this.GetSouthernCell(c);
        Cell w = this.GetWesternCell(c);

        if (n != null )
        {
            cells.Add(n);
        }
        if (e != null )
        {
            cells.Add(e);
        }
        if (s != null )
        {
            cells.Add(s);
        }
        if (w != null)
        {   
            cells.Add(w);
        }

        return cells;
    }

    /* ### HELPER METHODS ###*/
    private void DrawRun(List<Cell> run){
        int index = Random.Range(0, run.Count);
        for(int cell = 0; cell < run.Count; cell++) {
            if (cell != run.Count-1) {
                CarveOutEastWall(run[cell]);
            }
            if (cell == index) {
                CarveOutNorthWall(run[cell]);
            }
        }
    }

    private int DijkstraLongestPath()
    {
        int maxIndex = this.StartFlood(this.PickRandomCell());
        Cell begin = this.FindCellWithIndex(maxIndex);
        this.ResetDijkstraIndexxes();
        return this.StartFlood(begin);
    }

    private Cell FindCellWithIndex(int dijkstraIndex)
    {
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                if (this.grid[x, y].dijkstraIndex == dijkstraIndex -1)
                {
                    return this.grid[x, y];
                }
            }
        }

        //return garbage, index does not exists
        return PickRandomCell();
    }

    private void ResetDijkstraIndexxes()
    {
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                this.grid[x, y].dijkstraIndex = 0;
            }
        }
    }

    public void CarveWallBetween(Cell cell1, Cell cell2)
    {
        var deltaX = cell1.x - cell2.x;
        var deltaY = cell1.y - cell2.y;
        if (deltaX == -1 && deltaY == 0)
        {
            this.CarveOutEastWall(cell1);
        }
        else if (deltaX == 1 && deltaY == 0)
        {
            this.CarveOutWestWall(cell1);
        }
        else if (deltaY == 1 && deltaX == 0)
        {
            this.CarveOutSouthWall(cell1);
        }
        else if (deltaY == -1 && deltaX == 0)
        {
            this.CarveOutNorthWall(cell1);
        }
        else
        {
            Debug.Log("Cannot carve wall");
        }
    }

    public Cell PickRandomCell()
    {
        return this.grid[Random.Range(0, this.width), Random.Range(0, this.height)];
    }

    public Cell PickRandomUnvisitedCell()
    {
        List<Cell> cells = new List<Cell>();
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                if(!this.grid[x, y].isVisited)
                {
                    cells.Add(this.grid[x, y]);
                }
            }
        }
        
        return cells[Random.Range(0, cells.Count)];
    }

    public Cell MoveToRandomAdjacentCell(Cell c) 
    {
        Dictionary<string, Cell> cells = new Dictionary<string, Cell>();
        if (c.y != this.height-1) 
        {
            cells["North"] = this.grid[c.x, c.y + 1]; 
        }
        if (c.x != this.width - 1)
        {
            cells["East"] = this.grid[c.x + 1, c.y];
        }
        if (c.y != 0)
        {
            cells["South"] = this.grid[c.x, c.y - 1];
        }
        if (c.x != 0)
        {
            cells["West"] = this.grid[c.x - 1, c.y];
        }
        
        var direction = new List<string>(cells.Keys)[Random.Range(0, cells.Count)];
        if (!cells[direction].isVisited)
        {
            if (direction == "North")
            {
                this.CarveOutNorthWall(c);
            }
            else if (direction == "East")
            {
                this.CarveOutEastWall(c);
            }
            else if (direction == "South")
            {
                this.CarveOutSouthWall(c);
            }
            else // direction == "West"
            {
                this.CarveOutWestWall(c);
            }
        }

        return cells[direction];
    }

    public void CarveOutDirection(Cell c, string direction)
    {
        if (direction.Equals("North"))
        {
            this.CarveOutNorthWall(c);
        }
        else if (direction.Equals("East"))
        {
            this.CarveOutEastWall(c);
        }
        else if (direction.Equals("South"))
        {
            this.CarveOutSouthWall(c);
        }
        else //direction.Equals("West")
        {
            this.CarveOutWestWall(c);
        }
    }

    public void CarveOutNorthWall(Cell c)
    {
        if (c.y == this.height - 1) 
        {
            // cannot remove outer wall
            return;
        }
        c.north.isVisible = false;
    }

    public void CarveOutWestWall(Cell c)
    {
        var c2 = this.GetWesternCell(c);
        if (c2 == null) 
        {
            // cannot remove outer wall
            return;
        }
        c2.east.isVisible = false;
    }

    public void CarveOutSouthWall(Cell c)
    {
        var c2 = this.GetSouthernCell(c);
        if (c2 == null)
        {
            // cannot remove outer wall
            return;
        }
        c2.north.isVisible = false;
    }

    public void CarveOutEastWall(Cell c)
    {
        if (c.x == this.width -1)
        {
            // cannot remove outer wall
            return;
        }
        c.east.isVisible = false;
    }

    public Cell GetNorthernCell(Cell c)
    {
        if (c.y == this.height - 1)
        {
            // outer cell
            return null;
        }
        return this.grid[c.x, c.y + 1];
    }

    public Cell GetWesternCell(Cell c)
    {
        if (c.x == 0)
        {
            // outer cell
            return null;
        }
        return this.grid[c.x - 1, c.y];
    }

    public Cell GetSouthernCell(Cell c)
    {
        if (c.y == 0)
        {
            // outer cell
            return null;
        }
        return this.grid[c.x, c.y - 1];
    }

    public Cell GetEasternCell(Cell c)
    {
        if (c.x == this.width - 1)
        {
            // outer cell
            return null;
        }
        return this.grid[c.x + 1, c.y];
    }

    public bool HasNorthernWall(Cell c)
    {
        return (c.north != null && c.north.isVisible);
    }

    public bool HasEasternWall(Cell c)
    {
        return (c.east != null && c.east.isVisible);
    }

    public bool HasSouthernWall(Cell c)
    {
        return (c.y != 0 && HasNorthernWall(GetSouthernCell(c))); 
    }

    public bool HasWesternWall(Cell c)
    {
        return (c.x != 0 && HasEasternWall(GetWesternCell(c))); 
    }

    public Dictionary<string, Cell> GetNonVisitedNeighbours(Cell c)
    {
        Dictionary<string, Cell> neighbours = new Dictionary<string, Cell>();
        Cell n = this.GetNorthernCell(c);
        Cell e = this.GetEasternCell(c);
        Cell s = this.GetSouthernCell(c);
        Cell w = this.GetWesternCell(c);

        if (n != null && !n.isVisited)
        {
            neighbours["North"] = n;
        }
        if (e != null && !e.isVisited)
        {
            neighbours["East"] = e;
        }
        if (s != null && !s.isVisited)
        {
            neighbours["South"] = s;
        }
        if (w != null && !w.isVisited)
        {   
            neighbours["West"] = w;
        }

        return neighbours;
    }

    public bool AllCellsAreVisited()
    {
        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                if(!this.grid[x, y].isVisited) 
                {
                    return false;
                }
            }
        }
        return true;
    }

    public int StartFlood(Cell cell)
    {
        cell.dijkstraIndex = 1;
        var cells = new List<Cell>();
        cells.Add(cell);

        int maxInt = this.DijkstraFlood(cells, 1);

        for(int x = 0; x < this.width; x++)
        {
            for(int y = 0; y < this.height; y++)
            {
                this.setCellColor(grid[x,y], maxInt);
            }
        }

        return maxInt;
    }

    private int DijkstraFlood(List<Cell> cells, int i)
    {      
	if (cells.Count == 0)
	{
	    return i;
	}
	i++;
        var neigbhors = new List<Cell>();
        foreach(Cell c in cells) {
            neigbhors.AddRange(FloodToNeigbours(c));
        } 
        
        return this.DijkstraFlood(neigbhors, i);
    }

    private void setCellColor(Cell c, int maxWaarde ) {
        Vector3 startRGB = new Vector3(145, 0, 255);
        Vector3 maxRGB = new Vector3(214, 159, 255);
        Vector3 change = maxRGB - startRGB;
        Vector3 color = maxRGB - (change / (maxWaarde)) * (c.dijkstraIndex);
	if (c.dijkstraIndex == 0) 
	{
	    return;
	}
	c.color = new Color(color.x/255, color.y/255, color.z/255, 1);
    }

    private List<Cell> FloodToNeigbours(Cell c)
    {
        var neigbhors = new List<Cell>();
        if (c.y != this.height - 1 && !HasNorthernWall(c)) 
        {
            var n = GetNorthernCell(c);
            if (n.dijkstraIndex == 0) 
            { 
                n.dijkstraIndex = c.dijkstraIndex + 1;
                neigbhors.Add(n);
            } 
        }
        if (c.x != this.width - 1 && !HasEasternWall(c)) 
        {
            var e = GetEasternCell(c);
            if (e.dijkstraIndex == 0) 
            { 
                e.dijkstraIndex = c.dijkstraIndex + 1;
                neigbhors.Add(e);
            } 
        }
        if (c.y != 0  && !HasSouthernWall(c)) 
        {
            var s = GetSouthernCell(c);
            if (s.dijkstraIndex == 0) 
            { 
                s.dijkstraIndex = c.dijkstraIndex + 1;
                neigbhors.Add(s);
            } 
        }
        if (c.x != 0  && !HasWesternWall(c)) 
        {
            var w = GetWesternCell(c);
            if (w.dijkstraIndex == 0) 
            { 
                w.dijkstraIndex = c.dijkstraIndex + 1;
                neigbhors.Add(w);
            } 
        }
        
        return neigbhors;
    }


    public class Cell 
    {
        public Wall north = null;
        public Wall east = null;
        public int x, y;
        public int dijkstraIndex = 0;
        public MazeManager mazeRef;
        public bool isVisited = false;
        public Color color;

        public GameObject g; 

        public Cell(int x, int y, MazeManager mazeRef)
        {   

            this.x = x;
            this.y = y;    
            this.g = new GameObject("X: "+this.x+", Y: "+this.y);
            this.mazeRef = mazeRef;
            this.color = new Color(0.5f, 0.5f, 0.5f,1);
            
            if (this.x != this.mazeRef.width - 1) 
            {
                this.east = new Wall(this.SouthEastPoint(), this.NorthEastPoint(), true);
            }
            if (this.y != this.mazeRef.height - 1) 
            {
                this.north = new Wall(this.NorthWestPoint(), this.NorthEastPoint(), true);
            }
            var s = g.AddComponent<SpriteRenderer>();
        }

        // Should only be called once
        public void DrawTile()
        {
            this.g.transform.position = new Vector2(this.x - (this.mazeRef.width/2)-0.5f, this.y - (this.mazeRef.height/2) + 0.5f);
            var s = g.GetComponent<SpriteRenderer>();
            s.color = this.color;
            s.sprite = this.mazeRef.sprite;
            if (this.east != null) 
            {
                this.east.DrawWall();
            }
            if (this.north != null)
            {
                this.north.DrawWall();
            }
        }

        public void DestroyTile() {
            GL.Clear(true, true, new Color(49, 77, 121, 0));
            Destroy(this.g, 0);
            if (this.east != null) 
            {
                Destroy(this.east.g, 0);
            }
            if (this.north != null)
            {
                Destroy(this.north.g, 0);
            }
        }

        public void ResetTile() {
            if (this.east != null) {
                this.east.ResetWall();
            }
            if (this.north != null) {
                this.north.ResetWall();
            }
            
            this.color = new Color(0.5f, 0.5f, 0.5f,1);
            this.dijkstraIndex = 0;
            this.isVisited = false;
        }

        public Vector3 NorthWestPoint() 
        {
            return new Vector3(this.x - (this.mazeRef.width/2)-1f, this.y + 1 - (this.mazeRef.height/2), -1);
        }

        public Vector3 NorthEastPoint() 
        {
            return new Vector3(this.x + 1 - (this.mazeRef.width/2)-1f, this.y + 1 - (this.mazeRef.height/2), -1);
        }

        public Vector3 SouthWestPoint()
        {
            return new Vector3(this.x - (this.mazeRef.width/2)-1f, this.y - (this.mazeRef.height/2), -1);
        }

        public Vector3 SouthEastPoint() 
        {
            return new Vector3(this.x + 1 - (this.mazeRef.width/2)-1f, this.y - (this.mazeRef.height/2), -1);
        }
    }

    public class Wall
    {
        // AKA isNotPassage
        public bool isVisible;
        public GameObject g;
        public Vector3 start;
        public Vector3 eind;

        public Wall(Vector3 start, Vector3 eind, bool visible)
        {
            this.isVisible = visible;
            this.start = start;
            this.eind = eind;
            this.g = new GameObject();
            var l = this.g.AddComponent<LineRenderer>();
        }

        // Should only be called once
        public void DrawWall()
        {
            if (!this.isVisible) 
            {
                return;
            }
            var l = this.g.GetComponent<LineRenderer>();
            l.startWidth = 0.1f;
            l.positionCount = 2;
            l.SetPosition(0, this.start);
            l.SetPosition(1, this.eind);
        }

    	public void ResetWall() {
            var l = this.g.GetComponent<LineRenderer>();
            l.positionCount = 0;
            this.isVisible = true;
        }

    }
}
