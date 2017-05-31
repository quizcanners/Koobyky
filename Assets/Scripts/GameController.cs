using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

[Serializable]
public class SaveData {
    public  sector[,] grid;
    public int score;
    public int blocksToPlace;
    public int[] givenBlocks = new int[4];
 }

[Serializable]
public class sector {

    public static List<group> groups = new List<group>();

    public class group {
        public List<sector> list = new List<sector>();
    }

    [NonSerialized]
    public group myGroup;

    public int x;
    public int y;
    [NonSerialized]
    public BlockScript myBlock;
    public int color;


    public bool nextTo (sector other) {
        return (((other.x == x) || (other.y == y)) && ((Mathf.Abs(x - other.x) == 1) || (Mathf.Abs(y - other.y) == 1)));

    }
    public delegate bool groupingCondition(sector a, sector b);
    public static void groupSectors(groupingCondition cnd, ref sector[,] grid, int gridsize) {
        foreach (sector s in grid) 
            s.myGroup = null;
          
        groups.Clear();

        for (int x = 0; x < gridsize; x++)
            for (int y = 0; y < gridsize; y++) {
                sector s = grid[x, y];
                if ((x < gridsize - 1) && (cnd(s, grid[x + 1, y])))
                    checkGroups(s, grid[x + 1, y]);
                if ((y < gridsize - 1) && (cnd(s, grid[x, y + 1])))
                    checkGroups(s, grid[x, y + 1]);
            }

    }
    static void checkGroups(sector a, sector b)  {

        if ((a.myGroup != null) && (b.myGroup != null)) mergeGroups( a.myGroup,  b.myGroup);
        else {

            if (a.myGroup != null)  {
                a.myGroup.list.Add(b);
                b.myGroup = a.myGroup;
            }
            else if (b.myGroup != null)
            {
                b.myGroup.list.Add(a);
                a.myGroup = b.myGroup;
            }
            else
            {
                group  tmp = new group();
                tmp.list.Add(a);
                tmp.list.Add(b);
                a.myGroup = tmp;
                b.myGroup = tmp;
                sector.groups.Add(tmp);
            }
        }




    }
    static void mergeGroups(group a, group b) {

        if (a == b)
            return;

        foreach (sector s in b.list) {
            s.myGroup = a;
            a.list.Add(s);
        }

        b.list.Clear();
    }
    public sector(int nx, int ny) {
        x = nx;
        y = ny;
    }
}

public class GameController : MonoBehaviour {
    public Color[] colors = { Color.blue, Color.yellow, Color.green };
    public Material blockMaterial;
    public Transform lineupPosition;
    public GameObject blockPrefab;
    public MeshCollider _collider;
    public TextMesh scoreText;

    public int gridsize = 5;
    public int chanceFor1_4Block = 20;

    sector[,] grid;

    Material[] materials;

    List<BlockScript> blocks = new List<BlockScript>();
    int freeInPool;

    BlockScript[] givenBlocks = new BlockScript[4];
    int blocksToPlace = 0;
    int currentPlacingBlock = 0;
  
    sector pointedSector;
    bool placing = false;

    int score = 0;

    void SetSectorValue(sector sec, BlockScript b) {
        sec.myBlock = b;
        if (b != null) {
            b.mySector = sec;
            b.transform.position = sectorToPosition(sec);
            b.transform.localScale = Vector3.one;
        }
    }

    BlockScript getSectorValue (int x, int y) {
        if ((x < gridsize) && (y < gridsize))
            return grid[x, y].myBlock;
        else return null;
    }

    Vector3 sectorToPosition (sector sec) {
        float d = (gridsize - 1f) * 0.5f;
        return  (Vector3.zero + Vector3.up *   (d - sec.y) +  Vector3.right* (d - sec.x))*2;
    }

    void updatePointedSector() {
        RaycastHit hit;
        if (_collider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 9999)) {
            Vector2 pos = hit.textureCoord * gridsize;
            pointedSector = grid[(int)pos.x, (int)pos.y];
        }
    }

    bool isCellMate() {
        return ((currentPlacingBlock == 0) || (givenBlocks[currentPlacingBlock-1].mySector.nextTo(pointedSector)));
    }

    bool TryPlace() {
        updatePointedSector();

        BlockScript tmp = (pointedSector!= null) ? pointedSector.myBlock : null;

        if ((tmp == null) && (isCellMate()) && ((currentPlacingBlock < blocksToPlace)))  {
            SetSectorValue(pointedSector, givenBlocks[currentPlacingBlock]);
            currentPlacingBlock++;
            return true;
        }

        if ((tmp != null) && (currentPlacingBlock > 1) && (tmp == givenBlocks[currentPlacingBlock - 2])) {
            BlockScript rem = givenBlocks[currentPlacingBlock - 1];
            SetSectorValue(rem.mySector, null);
            BlockToLineup(rem, currentPlacingBlock - 1);
        }

        return false;
    }

    private void OnMouseDown() {
        if  (TryPlace())
            placing = true;
    }

    private void OnMouseUp() {

        if (currentPlacingBlock >= blocksToPlace) { TryExplode(); GiveNewBlocks(); placing = false; }

        for (int i= currentPlacingBlock-1; i>=0; i--) {

            SetSectorValue(givenBlocks[i].mySector, null);
            BlockToLineup(givenBlocks[i], i);
        }

        currentPlacingBlock = 0;
        placing = false;

    }

    BlockScript getBlock (int colorIndex) {
        BlockScript b = null;

        while (freeInPool < blocks.Count) {
           
            if (!blocks[freeInPool].gameObject.activeSelf) {
                b = blocks[freeInPool];
                break;
            }

            freeInPool++;
        }

        if (b == null) {
            b = Instantiate<GameObject>(blockPrefab).GetComponent<BlockScript>();
            b.myIndex = blocks.Count;
            blocks.Add(b);
        }

        b.rendy.sharedMaterial = materials[colorIndex];
        b.myColor = colorIndex;
        b.gameObject.SetActive(true);

        return b;
    }

    void DestroyBlock(BlockScript bs) {
        bs.gameObject.SetActive(false);
        freeInPool = Mathf.Min(freeInPool, bs.myIndex);
    }

    void ScoreBlock (BlockScript bs, int multiplier) {
        score += multiplier;
        SetSectorValue(bs.mySector, null);
        DestroyBlock(bs);
    }

    void BlockToLineup (BlockScript bs, int i) {
        
        bs.transform.localScale = Vector3.one * 0.5f;
        Vector3 pos = lineupPosition.position;
        pos.x += i - (blocksToPlace - 1f) * 0.5f;
        bs.transform.position = pos;
        givenBlocks[i] = bs;
        currentPlacingBlock = Mathf.Min(i, currentPlacingBlock);
    }

    bool CompareColors (sector a, sector b) {
        return ((a.myBlock != null) && (b.myBlock != null) && (a.myBlock.myColor == b.myBlock.myColor));
    }

    bool groupEmpty(sector a, sector b)
    {
        return ((a.myBlock == null) && (b.myBlock == null));
    }

    int BiggestRoom( ) {
        sector.groupSectors(groupEmpty, ref grid, gridsize);

        int max = 0;
        foreach (sector.group gr in sector.groups) {
            int cnt = gr.list.Count;
            int ends = 0;
            
            foreach (sector s in gr.list) {
                int friends = 0;
                foreach (sector s1 in gr.list) 
                    if ((s != s1) && (s.nextTo(s1))) friends++;

                if (friends == 1) ends++;
            }

            if (ends > 2) {
#if UNITY_EDITOR
                if (cnt == 4) Debug.Log("Culling T shaped room");
                if (cnt == 5) Debug.Log("Culling + shaped room");
#endif
                cnt -= (ends - 2);
            }


            max = Mathf.Max(max, cnt);
        }
        

        return max;
    }
    
    
/*  int BiggestRoom( ) {
        sector.groupSectors(groupEmpty, ref grid, gridsize);

        int max = 0;
        foreach (sector.group gr in sector.groups) 
            max = Mathf.Max(max, gr.list.Count);
        

        return max;
    }*/

    void TryExplode () {
        sector.groupSectors(CompareColors, ref grid, gridsize);

        foreach (sector.group gr in sector.groups) {
            int count = gr.list.Count;
            if (count > 2) 
                foreach (sector s in gr.list)
                    ScoreBlock(s.myBlock, count);
        }

        scoreText.text = score.ToString();
    }

    void GiveNewBlocks() {

        int max = BiggestRoom();

        if (max == 0) { StartGame(); return;}

        bool oneFour = UnityEngine.Random.Range(0, 100)< chanceFor1_4Block;

        blocksToPlace = (oneFour ? 1 : 2) + (UnityEngine.Random.Range(0, 2))*(oneFour ? 3 : 1);

        blocksToPlace = Mathf.Min(max, blocksToPlace);

        for (int i=0; i< blocksToPlace; i++)
            BlockToLineup(getBlock(UnityEngine.Random.Range(0, colors.Length)), i);

    }

    void ClearBlocks() {
        foreach (BlockScript r in blocks)
            r.gameObject.SetActive(false);
        freeInPool = 0;

        for (int x = 0; x < gridsize; x++)
            for (int y = 0; y < gridsize; y++)
                grid[x, y].myBlock = null;
    }

    void StartGame() {
        ClearBlocks();
        GiveNewBlocks();
        score = 0;
        scoreText.text = score.ToString();
    }

    void Start () {

        int cnt = colors.Length;
 
        materials = new Material[colors.Length];

        for (int i = 0; i < cnt; i++) {
            materials[i] = Instantiate<Material>(blockMaterial);
            materials[i].SetColor("_Color", colors[i]);
        }

        if (!LoadGame()) {
            grid = new sector[gridsize, gridsize];

            for (int x = 0; x < gridsize; x++)
                for (int y = 0; y < gridsize; y++)
                    grid[x, y] = new sector(x, y);

            StartGame();
        }
    }

    private void Update() {
        if (placing)
            TryPlace();
    }

    bool LoadGame() {
        if (File.Exists(Application.persistentDataPath + "/savedGame.gd"))  {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/savedGame.gd", FileMode.Open);
            SaveData sd = (SaveData)bf.Deserialize(file);
            file.Close();

            grid = sd.grid;
            blocksToPlace = sd.blocksToPlace;
            for (int i=0; i<blocksToPlace; i++)
                BlockToLineup(getBlock(sd.givenBlocks[i]), i);
            foreach(sector s in grid) if (s.color != -1)
                SetSectorValue(s, getBlock(s.color));

            score = sd.score;

            scoreText.text = score.ToString();

            return true;
        }
        return false;
    }

    private void OnApplicationQuit() {
        SaveData sd = new SaveData();

        foreach (sector s in grid) s.color = (s.myBlock == null) ? -1 : s.myBlock.myColor;
        sd.grid = grid;
        sd.score = score;
        sd.blocksToPlace = blocksToPlace;
        for (int i = 0; i < blocksToPlace; i++)
            sd.givenBlocks[i] = givenBlocks[i].myColor;

    
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/savedGame.gd");
        bf.Serialize(file, sd);
        file.Close();

    }

   

}
