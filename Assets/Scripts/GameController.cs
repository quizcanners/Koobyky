using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using QuizCannersUtilities;

public class GameController : MonoBehaviour {
    public int pixelsLayer;
    public int explodedPixelsLayer;

    public Color[] colors = { Color.blue, Color.yellow, Color.green };
    public Material blockMaterial;
   

    public Transform lineupPosition;
    public GameObject blockPrefab;
    public MeshCollider _collider;
    public ScoreTextMGMT scoreText;

    // _BG_GRAD_COL_1("Background Upper", Color) = (1,1,1,1)
    //   _BG_CENTER_COL("Background Center", Color) = (1,1,1,1)
    //  _BG_GRAD_COL_2("Background Lower", Color) = (1,1,1,1)

    [Header("Gradient BG & Outline")]
    const float bgSpeed = 0.2f;
    private LinkedLerp.MaterialColor color0 = new LinkedLerp.MaterialColor("_BG_GRAD_COL_1", Color.black, startingSpeed: bgSpeed);
    private LinkedLerp.MaterialColor color1 = new LinkedLerp.MaterialColor("_BG_CENTER_COL", Color.black, startingSpeed: bgSpeed);
    private LinkedLerp.MaterialColor color2 = new LinkedLerp.MaterialColor("_BG_GRAD_COL_2", Color.black, startingSpeed: bgSpeed);
    private LinkedLerp.MaterialColor outline = new LinkedLerp.MaterialColor("_OutlineColor", Color.clear, startingSpeed: 4f);

 

    [Header("Audio")]

    public AudioClip startSound;
    public AudioClip firstBlockSound;
    public AudioClip newBlockSound;
    public AudioClip finalBlockSound;
    public AudioClip scoreSound;
    public AudioClip noScoreSound;
    public AudioClip backSound;
    public AudioClip mouseUpUnfinishedSound;
    public AudioClip newGameSound;
  

    public AudioSource audioSource;

    [Header("Grid")]
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
            b.gameObject.layer = pixelsLayer;
        }
        UpdateBG();
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

    Vector2 pressurePos = new Vector2();
    float pressure = 0;
    void updatePointedSector() {
        if (placing) pressure = Mathf.Lerp(pressure, 1, Time.deltaTime*5);
        else pressure = Mathf.Lerp(pressure, 0, Time.deltaTime*0.5f);

        RaycastHit hit;
        if (_collider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 9999)) {
            pressurePos = hit.textureCoord;
            Vector2 pos = hit.textureCoord * gridsize;
            pointedSector = grid[(int)pos.x, (int)pos.y];
        }

        Shader.SetGlobalVector("_touchPoint", new Vector4(pressurePos.x, pressurePos.y, pressure, 0));
    }

    bool isCellMate() {
        return ((currentPlacingBlock == 0) || (givenBlocks[currentPlacingBlock-1].mySector.nextTo(pointedSector)));
    }
    
    void UpdateBG() {
        CameraScaler.inst.RenderTexCamera.Render();
    }
    
    bool TryPlace() {
        updatePointedSector();
        BlockScript tmp = (pointedSector!= null) ? pointedSector.myBlock : null;

        if ((tmp == null) && (isCellMate()) && ((currentPlacingBlock < blocksToPlace)))  {
            SetSectorValue(pointedSector, givenBlocks[currentPlacingBlock]);
            currentPlacingBlock++;
            if (currentPlacingBlock < blocksToPlace)
            {
                //bgColor(givenBlocks[currentPlacingBlock]);
                if (currentPlacingBlock == 1)
                    audioSource.PlayOneShot(firstBlockSound, 1f);
                else
                    audioSource.PlayOneShot(newBlockSound,1f);
            }
            else
            {
                //bgColor(Color.black);
                audioSource.PlayOneShot(finalBlockSound,1f);
            }

            return true;
        }

        if ((tmp != null) && (currentPlacingBlock > 1) && (tmp == givenBlocks[currentPlacingBlock - 2])) {
            BlockScript rem = givenBlocks[currentPlacingBlock - 1];
            BlockToLineup(rem, currentPlacingBlock - 1);
            audioSource.PlayOneShot(backSound, 1f);
            //bgColor(rem);
            SetSectorValue(rem.mySector, null);
        }

        return false;
    }

    private void OnMouseDown() {
        if  (TryPlace())
            placing = true;
    }

    private void OnMouseUp() {

        if (currentPlacingBlock >= blocksToPlace) { TryExplode(); GiveNewBlocks(); placing = false; }

        if (currentPlacingBlock > 0)
            audioSource.PlayOneShot(mouseUpUnfinishedSound);

        for (int i= currentPlacingBlock-1; i>=0; i--) {
            BlockToLineup(givenBlocks[i], i);
            SetSectorValue(givenBlocks[i].mySector, null);
          
        }
        
        UpdateBG();
        currentPlacingBlock = 0;
        placing = false;

    }

    private Color GetPreviewColor(int blockIndex)
    {
        
        blockIndex += currentPlacingBlock;

        if (blockIndex >= blocksToPlace)
            return Color.black;

        var col = colors[givenBlocks[blockIndex].myColor];

        col = Color.LerpUnclamped(col, Color.black, 0.5f);

        return col;
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
        
        bs.transform.localScale = Vector3.one * 0.1f;
        Vector3 pos = lineupPosition.position;
        pos.x += i;
            //i - (blocksToPlace - 1f) * 0.5f;
        bs.transform.position = pos;
        givenBlocks[i] = bs;
        currentPlacingBlock = Mathf.Min(i, currentPlacingBlock);
        bs.gameObject.layer = 0;
        
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

            if (ends > 2)
            {
#if UNITY_EDITOR
               // if (cnt == 4) Debug.Log("Culling T shaped room");
              //  if (cnt == 5) Debug.Log("Culling + shaped room");
#endif
                cnt -= (ends - 2);
            }


            max = Mathf.Max(max, cnt);
        }
        

        return max;
    }

    void TryExplode () {
        sector.groupSectors(CompareColors, ref grid, gridsize);

        int groups = 0;
        int blocks = 0;


        foreach (sector.group gr in sector.groups) {
            int count = gr.list.Count;
            if (count > 2) {
                foreach (sector s in gr.list)
                    s.myBlock.gameObject.layer = explodedPixelsLayer;

                    blocks += count; groups++; }
        }

        ExplodedPixelsCamera.Render();

        int multiplier = groups * blocks;
        if (multiplier > 0) {
            shotsToDo += 1 * groups + multiplier / 8;
            //audioSource.PlayOneShot(scoreSound);
        }


        foreach (sector.group gr in sector.groups) {
            if (gr.list.Count > 2) 
                foreach (sector s in gr.list)
                    ScoreBlock(s.myBlock, multiplier);
        }

        scoreText.targetScore = score;
    }
    
    void GiveNewBlocks() {

        int max = BiggestRoom();

        if (max == 0) {

            try {
                //IndiexpoAPI_WebGL.SendScore(score);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
            StartGame(); return;}

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
        scoreText.targetScore = 0;
        UpdateBG();
        audioSource.PlayOneShot(newGameSound);
    }

    void Start () {

        if (!audioSource)  {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (!audioSource)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        int cnt = colors.Length;
 
        materials = new Material[colors.Length];

        for (int i = 0; i < cnt; i++) {
            materials[i] = Instantiate(blockMaterial);
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

    float shotsDelay = 0;
    float shotsToDo = 0;

    private void Update() {

        LerpData lr = new LerpData();

        color0.Portion(lr, GetPreviewColor(1)); // = );
        //color1.Portion(lr, GetColor(1));
        color2.Portion(lr, GetPreviewColor(2));
        outline.Portion(lr, GetPreviewColor(0));

        color0.Lerp(lr);
        //color1.Lerp(lr);
        color2.Lerp(lr);
        outline.Lerp(lr);

            
        shotsDelay -= Time.deltaTime;

        if ((shotsToDo>0) && (shotsDelay < 0)) {
            audioSource.PlayOneShot(scoreSound);
            shotsDelay = 0.02f;
            shotsToDo--;
        }


        if (placing) 
            TryPlace();

#if !UNITY_ANDROID
        updatePointedSector();
#endif



        for (int i= currentPlacingBlock; i<blocksToPlace; i++) {
            BlockScript b = givenBlocks[i];
            float scale = b.transform.localScale.x;
            scale = Mathf.Lerp(scale, (i == currentPlacingBlock) ? 1 : 0.5f, Time.deltaTime * 10);
            b.transform.localScale = Vector3.one * scale;
        }




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

            scoreText.Restart(score);

            return true;
        }
        return false;
    }

    public void OnApplicationFocus(bool focus) {
        if (!focus) Save();
    }

    public void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    public void OnApplicationQuit()  {

        Save();


    }

   void Save()
    {
        SaveData sd = new SaveData();

        foreach (sector s in grid)
            s.color = (s.myBlock == null) ? -1 : s.myBlock.myColor;

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

[Serializable]
public class SaveData
{
    public sector[,] grid;
    public int score;
    public int blocksToPlace;
    public int[] givenBlocks = new int[4];
}

[Serializable]
public class sector
{

    public static List<group> groups = new List<group>();

    public class group
    {
        public List<sector> list = new List<sector>();
    }

    [NonSerialized]
    public group myGroup;

    public int x;
    public int y;
    [NonSerialized]
    public BlockScript myBlock;
    public int color;


    public bool nextTo(sector other)
    {
        return (((other.x == x) || (other.y == y)) && ((Mathf.Abs(x - other.x) == 1) || (Mathf.Abs(y - other.y) == 1)));

    }
    public delegate bool groupingCondition(sector a, sector b);
    public static void groupSectors(groupingCondition cnd, ref sector[,] grid, int gridsize)
    {
        foreach (sector s in grid)
            s.myGroup = null;

        groups.Clear();

        for (int x = 0; x < gridsize; x++)
            for (int y = 0; y < gridsize; y++)
            {
                sector s = grid[x, y];
                if ((x < gridsize - 1) && (cnd(s, grid[x + 1, y])))
                    checkGroups(s, grid[x + 1, y]);
                if ((y < gridsize - 1) && (cnd(s, grid[x, y + 1])))
                    checkGroups(s, grid[x, y + 1]);
            }

    }
    static void checkGroups(sector a, sector b)
    {

        if ((a.myGroup != null) && (b.myGroup != null)) mergeGroups(a.myGroup, b.myGroup);
        else
        {

            if (a.myGroup != null)
            {
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
                group tmp = new group();
                tmp.list.Add(a);
                tmp.list.Add(b);
                a.myGroup = tmp;
                b.myGroup = tmp;
                sector.groups.Add(tmp);
            }
        }




    }
    static void mergeGroups(group a, group b)
    {

        if (a == b)
            return;

        foreach (sector s in b.list)
        {
            s.myGroup = a;
            a.list.Add(s);
        }

        b.list.Clear();
    }
    public sector(int nx, int ny)
    {
        x = nx;
        y = ny;
    }
}
