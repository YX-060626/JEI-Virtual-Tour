using UnityEngine;

public static partial class JEIStructureBlockout
{
    static void Classroom(Room r,bool groups)
    {
        float door=firstDoors[r.id];float cx=(r.x0+r.x1)/2;
        if(!groups)
        {
            for(float x=r.x0+1.05f;x<r.x1-.8f;x+=1.7f)
            { if(Mathf.Abs(x-door)<1.4f)continue;
                for(int row=0;row<3;row++){float z=8.1f+row*1.45f;Desk(x,z,180,false,1.15f,yellow);Chair(x,z+.66f,180,blue);} }
        }
        else
        {
            foreach(float x in new[]{r.x0+1.65f,r.x1-1.65f})
                foreach(float z in new[]{8.4f,10.65f})
                { Desk(x,z,0,false,1.8f,white); Chair(x-.5f,z-.67f,0,yellow);Chair(x+.5f,z+.67f,180,blue); }
        }
        float bx=Mathf.Abs(cx-door)>1.8f?cx:r.x0+1.9f;
        Board(bx,6.035f,180,groups?"DISCUSS / CREATE":"LEARN / EXPLORE",teal,2.8f);
        Shelf(r.x0+1.1f,12.34f,0,1.55f);Plant(r.x1-.6f,12.1f);
        if(!groups)Desk(r.x0+1.2f,6.9f,0,true,1.35f);
    }
    static void ComputerCollaboration(Room r)
    {
        float door=firstDoors[r.id];
        for(float x=r.x0+1.1f;x<r.x1-.8f;x+=1.55f)
        { if(Mathf.Abs(x-door)<1.45f)continue;Desk(x,11.95f,0,true);Chair(x,11.22f,0,metal); }
        foreach(float x in new[]{1.25f,7.9f})
        { Desk(x,8.85f,0,false,2.3f,white);Chair(x-.7f,8.12f,0,blue);Chair(x+.7f,8.12f,0,yellow);Chair(x-.7f,9.58f,180,blue);Chair(x+.7f,9.58f,180,yellow); }
        Board(.95f,6.04f,180,"COLLABORATION LAB",teal,2.35f);Shelf(9.9f,7.65f,90,1.5f);Plant(9.65f,12.0f);
    }
    static void Seminar(Room r)
    {
        Desk(15.45f,10.8f,90,false,2.5f,oak);Chair(16.2f,10.1f,270);Chair(16.2f,11.45f,270);
        Desk(13.2f,11.8f,0,false,2.5f,oak);Chair(12.55f,11.05f,0,yellow);Chair(13.9f,11.05f,0,yellow);
        Desk(11.55f,9.7f,270,false,2.4f,oak);Chair(12.3f,9.2f,270);Chair(12.3f,10.25f,270);
        Board(15.5f,6.04f,180,"SEMINAR / IDEAS",blue,2.5f);Plant(17.1f,12.1f);Shelf(17.25f,8.1f,90,1.5f);
    }
    static void TeachersOffice(Room r)
    {
        foreach(float x in new[]{-15.9f,-13.8f,-11.7f,-5.4f})
        {Desk(x,-11.7f,180,true,1.6f);Chair(x,-10.95f,180,metal);}
        Sofa(-15.9f,-7.2f,180,blue,2.1f);Sofa(-12.9f,-7.2f,180,yellow,2.1f);CoffeeTable(-14.4f,-8.5f);
        Desk(-5.55f,-8.6f,0,false,2.7f,oak);Chair(-6.35f,-9.35f,0);Chair(-4.8f,-9.35f,0);Chair(-6.35f,-7.85f,180);Chair(-4.8f,-7.85f,180);
        Shelf(-17.22f,-9.5f,270,1.7f,false);Plant(-17.1f,-6.65f);Plant(-3.95f,-12f);
        Board(-11f,-6.04f,0,"STAFF NOTICEBOARD",yellow,2.2f);
    }
    static void Reception(Room r)
    {
        var g=Item("Reception counter",-2.18f,-8.25f);
        D(g,"Counter front",new Vector3(0,.53f,0),new Vector3(1.35f,1.06f,1.5f),oak);
        D(g,"Counter top",new Vector3(0,1.10f,0),new Vector3(1.47f,.09f,1.6f),white);
        D(g,"Counter inset",new Vector3(.69f,.62f,0),new Vector3(.02f,.40f,1.2f),teal);Solid(g,new Vector3(0,.6f,0),new Vector3(1.5f,1.2f,1.6f));
        Sofa(2.30f,-9.35f,270,purple,1.9f);CoffeeTable(2.1f,-11.05f);Plant(-2.55f,-11.8f);Plant(2.55f,-6.65f);
        Board(-2f,-6.04f,0,"WELCOME TO JEI",teal,1.7f);
        var water=Item("Water dispenser",-2.6f,-10.2f);D(water,"Dispenser base",new Vector3(0,.5f,0),new Vector3(.43f,1,.43f),white);
        Shape(water,"Water bottle",PrimitiveType.Cylinder,new Vector3(0,1.23f,0),new Vector3(.31f,.23f,.31f),blue);Solid(water,new Vector3(0,.73f,0),new Vector3(.45f,1.46f,.45f));
    }
    static void StudentServices(Room r)
    {
        foreach(float x in new[]{4.6f,8.5f}){Desk(x,-10.9f,180,true,1.7f);Chair(x,-10.16f,180,metal);}
        Sofa(8.6f,-7.2f,180,purple,1.65f);CoffeeTable(8.7f,-8.55f);
        Shelf(4.6f,-12.36f,180,1.6f,false);Plant(9.3f,-12f);
        Board(4.75f,-6.04f,0,"STUDENT SERVICES",terracotta,1.8f);
        var divider=Item("Desk privacy screen",8.5f,-11.35f);D(divider,"Fabric panel",new Vector3(0,1.12f,0),new Vector3(1.8f,.58f,.055f),teal);
    }
    static void MeetingRoom(Room r)
    {
        Desk(14.35f,-9.9f,90,false,3.5f,oak);
        foreach(float z in new[]{-11.1f,-9.9f,-8.7f}){Chair(13.58f,z,90,metal);Chair(15.1f,z,270,metal);}
        Board(15.6f,-6.04f,0,"MEETING / PRESENT",teal,2.5f);Shelf(11.1f,-12.35f,180,1.5f,false);Plant(17.1f,-12f);
    }
    static void ServiceRoom(Room r)
    {
        if(r.id=="C02" || r.id=="C03")
        {
            bool north=r.id=="C02";float z=north?.72f:-.72f;
            foreach(float x in new[]{-11.4f,-7.7f})
            {
                Toilet(x,z,north?180:0);
                D(decor,"WC partition",new Vector3(x+(x<-9?.66f:-.66f),.95f,north?1f:-1f),new Vector3(.045f,1.9f,1.6f),blue,true);
            }
            Sink(-7.15f,north?2.55f:-2.55f,90);
            D(decor,"Sanitary colour band",new Vector3(-9.5f,.65f,north?.12f:-.12f),new Vector3(5.5f,1.15f,.025f),north?purple:blue);
        }
        else if(r.id=="C07") {Toilet(7.2f,-.65f,0);Sink(8.85f,-1.8f,90);D(decor,"Accessible grab rail",new Vector3(6.85f,.78f,-.7f),new Vector3(.035f,.035f,.75f),metal);}
        else if(r.id=="C05")
        { Shelf(7.2f,.42f,180,.75f,false);Shelf(8.7f,.42f,180,.7f,false);Shape(decor,"Cleaning bucket",PrimitiveType.Cylinder,new Vector3(8.8f,.24f,1.4f),new Vector3(.35f,.23f,.35f),yellow);D(decor,"Mop handle",new Vector3(6.9f,.85f,1.4f),new Vector3(.035f,1.7f,.035f),oak); }
        else if(r.id=="C08")
        {
            foreach(float x in new[]{9.95f,11.85f})
            { var g=Item("IT equipment rack",x,-.7f);D(g,"Rack cabinet",new Vector3(0,1.05f,0),new Vector3(.58f,2.1f,.75f),metal,true);
                for(int i=0;i<7;i++){D(g,"Server unit",new Vector3(0,.28f+i*.23f,-.385f),new Vector3(.50f,.17f,.02f),tile);D(g,"Status LED",new Vector3(.17f,.28f+i*.23f,-.403f),new Vector3(.03f,.025f,.01f),teal);} }
        }
        else if(r.kind=="stair")
        {
            float x=r.id=="C01"?-12.52f:12.52f;var g=Item("Stair safety marker",x,0,r.id=="C01"?90:270);
            D(g,"Stair marker panel",new Vector3(0,1.8f,0),new Vector3(1.4f,.45f,.035f),teal);Label(g,"STAIRS / UP",new Vector3(0,1.8f,-.025f),.055f,Color.white);
        }
        else if(r.kind=="courtyard")
        {Plant(-2.85f,2.25f,1.25f);Plant(2.85f,2.25f,1.25f);Sofa(-2.8f,0,90,teal,1.7f);Sofa(2.8f,0,270,teal,1.7f);}
        else if(r.id=="C06")
        {
            D(decor,"Reserved lift door",new Vector3(10.9f,1.15f,3.3f),new Vector3(1.06f,2.3f,.10f),metal,true);
            D(decor,"Lift panel left",new Vector3(10.63f,1.15f,3.365f),new Vector3(.50f,2.20f,.02f),tile);
            D(decor,"Lift panel right",new Vector3(11.17f,1.15f,3.365f),new Vector3(.50f,2.20f,.02f),tile);
        }
    }
}
