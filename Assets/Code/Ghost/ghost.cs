using System;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;

internal struct QueueNode{
    public GameObject node;
    public int step;
    public int id;
    public QueueNode(GameObject node,int step,int id){
        this.node=node;
        this.step=step;
        this.id=id;//useless
    }
}

//used in A*
internal class PriorityQueue{
    List<QueueNode> queue;
    List<int> value;
    public int Count;

    public PriorityQueue(){
        queue=new List<QueueNode>();
        value=new List<int>();
        Count=0;
    }

    public void Clear(){
        queue.Clear();
        value.Clear();
        Count=0;
    }

    public void Enqueue(QueueNode a,int val){
        queue.Add(a);
        value.Add(val);

        int now=Count,par=(now-1)/2;
        QueueNode temT;
        int temInt;
        Count++;
        while(value[now]<value[par]){

            temT=queue[now];
            temInt=value[now];
            queue[now]=queue[par];
            value[now]=value[par];
            queue[par]=temT;
            value[par]=temInt;

            now=par;
            par=(now-1)/2;
        }
    }

    public QueueNode Dequeue(){
        if(Count==0){
            return default(QueueNode);
        }
        else{
            QueueNode retNode=queue[0],temNode;
            Count--;
            
            queue[0]=queue[Count];
            value[0]=value[Count];
            queue.RemoveAt(Count);
            value.RemoveAt(Count);
            int now,i=0,le=1,ri,temInt;
            while(le<Count){
                now=i;
                ri=le+1;
                if(value[le]<value[now]){
                    now=le;
                }
                if(ri<Count&&value[ri]<value[now]){
                    now=ri;
                }

                if(now==i){
                    break;
                }
                else{
                    temNode=queue[now];
                    temInt=value[now];
                    queue[now]=queue[i];
                    value[now]=value[i];
                    queue[i]=temNode;
                    value[i]=temInt;

                    i=now;
                    le=(i*2)+1;
                }
            }
            return retNode;
        }
    }

}


public abstract class ghost:entity,Ighost{
    protected const int NOT_FOUND=-1,SAME_NODE=-2;
    public static GameObject target;
    public static pacman_control pacman;
    public static Sprite bodyAfraid;
    public static Sprite[] eyes;

    public delegate bool Comparator(GameObject node);
    
    [SerializeField]protected int searchRange;//used in pathfinding
    [SerializeField]protected int respawnDirection;//used in respawn
    protected bool isEdible=false,pacmanFound; //used when energizers are eaten
    protected int respawnTime;

    [Header("sprites")]
    [SerializeField]protected Sprite bodyNormal;
    [Header("SpriteRenderer")]
    [SerializeField]protected SpriteRenderer eyesRenderer; //used for animation of eyes
    [SerializeField]protected SpriteRenderer bodyRenderer; //used for animation of body movement


    private Dictionary<GameObject,int> visited=new Dictionary<GameObject,int>();
    private Dictionary<GameObject,GameObject> getPath=new Dictionary<GameObject,GameObject>();
    private Queue<QueueNode> BFSQueue=new Queue<QueueNode>();
    private PriorityQueue AStarQueue=new PriorityQueue();


    protected override void Start(){
        base.Start();
        direction=respawnDirection;
        speed=speedNormal;
        respawnTime=10*manager.frameRate;
    }//general start() method, each ghost will have their own start() method
    
    //check whether the ghost can move in update()
    protected bool CanUpdate(){
        if(manager.gameActive==false){
            return false;
        }
        else if(countDown>0){
            countDown--;
            bodyRenderer.color=new Color(1,1,1,((float)(respawnTime-countDown))/respawnTime);
            if(countDown==0){GetComponent<BoxCollider2D>().enabled=true;}
            return false;
        }
        return CanChangeNode();
    }

    //general update() method, each ghost will have their own update() method
    protected virtual void Update(){
        int nextDirection=pacmanFound?AStar():BFS();
        //switch to A* if pacman is found for performance
        if(nextDirection<0){//cant find pacman
            pacmanFound=false;
            direction=RandomMove();
        }
        else if(isEdible){
            pacmanFound=true;
            direction=Escape(3-nextDirection);
        }
        else{
            pacmanFound=true;
            direction=nextDirection;
        }
        eyesRenderer.sprite=eyes[direction];
        curNode=curNode.GetComponent<node_control>().NodeNearby[direction];
    }

    //check the if escape direction is valid
    protected virtual int Escape(int escapeDirection){
        if(curNode.GetComponent<node_control>().NodeNearby[escapeDirection]==null){
            return RandomMove();
        }
        else{
            return escapeDirection;
        }
    }


    int[] directionChoices=new int[4];
    protected int RandomMove(){
        int bannedDirection=3-direction;
        node_control controller=curNode.GetComponent<node_control>();
        int i,j;
        for(i=0,j=0;i<4;i++){
            if(i!=bannedDirection&&controller.NodeNearby[i]!=null){
                directionChoices[j++]=i;
            }
        }
        if(j==0){
            return bannedDirection;
        }
        else{
            return directionChoices[Random.Range(0,j)];
        }
    }//decide to movement of ghosts before seeing pacman

    //some ghost may have different change when levelup, so virtual function
    public override void LevelUp(){
        speed+=0.2f;
        speedNormal+=0.2f;
        speedFast+=0.2f;
        searchRange++;
        countDown=0;
        GetComponent<BoxCollider2D>().enabled=true;
        respawnTime-=manager.frameRate/2;
        bodyRenderer.color=Color.white;
        Restart();
    }

    //all ghosts are the same: translate to respawnNode and wait for count down
    public bool BeingEaten(){
        if(isEdible){
            Restart();
            bodyRenderer.color=new Color(1,1,1,0);
            countDown=respawnTime;
            GetComponent<BoxCollider2D>().enabled=false;
            return true;
        }
        else{
            return false;
        }
    }


    public void SetEdible(){
        if(countDown>0){return;}
        eyesRenderer.enabled=false;
        bodyRenderer.sprite=bodyAfraid;
        speed=speedNormal;
        isEdible=true;
    }//set the ghost to be edible after energizer is eaten


    public void UnsetEdible(){
        eyesRenderer.enabled=true;
        bodyRenderer.sprite=bodyNormal;
        isEdible=false;
    }//set the ghost to be unedible after energizer time is used up

    protected override void Restart(){
        base.Restart();
        UnsetEdible();
        direction=respawnDirection;
    }// reset the state of ghosts


    //return the direction the ghost should follow
    protected int BFS(){
        if(Reach(curNode)){
            return SAME_NODE;
        }
        visited.Clear();
        getPath.Clear();
        BFSQueue.Clear();

        int step=0,i,id=0,pid=0;
        node_control controller=curNode.GetComponent<node_control>();
        GameObject border=curNode;
        QueueNode headNode;

        visited.Add(curNode,1);
        getPath.Add(curNode,curNode);

        while(true){
            if(Reach(border)){
                while(getPath[border]!=curNode){
                    border=getPath[border];
                }
                controller=curNode.GetComponent<node_control>();
                for(i=0;i<4;i++){
                    if(controller.NodeNearby[i]==border)break;
                }
                return i;
            }
            else if(++step<searchRange){
                for(i=0;i<4;i++){
                    GameObject nextNode=controller.NodeNearby[i];
                    if(nextNode!=null&&visited.ContainsKey(nextNode)==false){
                        visited.Add(nextNode,1);
                        id++;
                        BFSQueue.Enqueue(new QueueNode(nextNode,step,id));
                        getPath.Add(nextNode,border);
                    }
                }
            }
            if(BFSQueue.Count<1){
                return NOT_FOUND;
            }
            headNode=BFSQueue.Dequeue();
            border=headNode.node;
            pid=headNode.id;
            step=headNode.step;
            controller=border.GetComponent<node_control>();
        }
    }


    protected int AStar(){
        if(Reach(curNode)){
            return SAME_NODE;
        }
        visited.Clear();
        getPath.Clear();
        AStarQueue.Clear();

        int step=0,i,id=0,pid=0;
        int estimate,previousEstimate;
        node_control controller=curNode.GetComponent<node_control>();
        GameObject border=curNode;
        QueueNode headNode;

        visited.Add(curNode,1);
        getPath.Add(curNode,curNode);

        while(true){
            if(Reach(border)){
                while(getPath[border]!=curNode){
                    border=getPath[border];
                }
                controller=curNode.GetComponent<node_control>();
                for(i=0;i<4;i++){
                    if(controller.NodeNearby[i]==border)break;
                }
                return i;
            }
            else if(++step<searchRange){
                visited[border]=-1;
                for(i=0;i<4;i++){
                    GameObject nextNode=controller.NodeNearby[i];
                    if(nextNode!=null){
                        estimate=step+Heuristic(border.transform.position);
                        if(visited.TryGetValue(nextNode,out previousEstimate)){
                            if(previousEstimate<=estimate){continue;}
                            visited[nextNode]=estimate;
                            AStarQueue.Enqueue(new QueueNode(nextNode,step,id),estimate);
                            getPath[nextNode]=border;
                        }
                        else{
                            visited.Add(nextNode,estimate);
                            id++;
                            AStarQueue.Enqueue(new QueueNode(nextNode,step,id),estimate);
                            getPath.Add(nextNode,border);
                        }
                    }
                }
            }
            if(AStarQueue.Count<1){
                return NOT_FOUND;
            }
            headNode=AStarQueue.Dequeue();
            border=headNode.node;
            pid=headNode.id;
            step=headNode.step;
            controller=border.GetComponent<node_control>();
        }
    }

    //Manhattan distance
    protected virtual int Heuristic(Vector2 nodePosition){
        int dx=(int)Math.Round(Math.Abs(nodePosition.x-target.transform.position.x),0);
        int dy=(int)Math.Round(Math.Abs(nodePosition.y-target.transform.position.y),0);
        return dx+dy;
    }

    //for determine the path finding is success
    protected virtual bool Reach(GameObject node){
        return node==target;
    }
    
}
