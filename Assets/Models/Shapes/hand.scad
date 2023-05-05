
module base()
{
    hull()
    {
        translate([2,0,0]) cylinder(r=37, h=10, center=true);  
        translate([-5,20,0]) cylinder(r=27, h=10, center=true); 
        translate([+15,15,0]) cylinder(r=32, h=10, center=true);
    }
      
    translate ([-60,20,-10]) rotate ([-25,0,0]) rotate([0,-35, 35])  scale([0.65,1.2,1])  cylinder(r=18, h=10, center=true);
    
    translate([-10,85,-15]) rotate ([-25,0,0])
    {
        translate ([-5,-5,0]) scale([.5,1.5,1]) cylinder(r=18, h=10, center=true);
        translate ([+18,0,0]) rotate([0,0,-5]) scale([.5,1.8,1]) cylinder(r=18, h=10, center=true);
        translate ([+42,-8,0]) rotate([0,0,-15]) scale([.45,1.5,1]) cylinder(r=18, h=10, center=true);
        translate ([+63,-20,-4]) rotate([0,0,-25]) scale([.4,1.35,1]) cylinder(r=18, h=10, center=true);
    }
    
}
 
module foot()
{
    minkowski()
    {
        sphere(r = 3.5, $fn = 8);
        translate([40,-90,40]) scale([.9,1,1])
        {
            base(); 
        }
    }
}

foot(); 
