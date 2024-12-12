shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform float colors : hint_range(1.0, 16.0);
uniform float dither : hint_range(0.0, 0.5);

uniform int chromaticLevels = 3;
uniform float chromaticSpread = 0.01;
uniform float chromaticRadiusScale = 0.5;
uniform float warp_amount : hint_range(-5.0, 5.0) = 0.1;

varying vec4 screenColor;
varying vec2 screen_uv;
varying vec2 uv;
varying vec2 texture_pixel_size;

vec3 calcDitherAndColorLimit() {
	vec3 clr = vec3(0.0);
	
	float a = floor(mod(uv.x / texture_pixel_size.x, 2.0));
	float b = floor(mod(uv.y / texture_pixel_size.y, 2.0));
	float c = mod(a + b, 2.0);
	
	float shiftedDither = dither;
	
	clr.r = (round(screenColor.r * colors + shiftedDither) / colors) * c;
	clr.g = (round(screenColor.g * colors + shiftedDither) / colors) * c;
	clr.b = (round(screenColor.b * colors + shiftedDither) / colors) * c;
	
	c = 1.0 - c;
	
	clr.r += (round(screenColor.r * colors - shiftedDither) / colors) * c;
	clr.g += (round(screenColor.g * colors - shiftedDither) / colors) * c;
	clr.b += (round(screenColor.b * colors - shiftedDither) / colors) * c;
	
	return clr;
}

vec3 chromatic_slice(float t){
	vec3 res = vec3(1.0-t, 1.0 - abs(t - 1.0), t - 1.0);
	return max(res, 0.0);
}

vec3 calcChromaticAbberation() {
	vec3 result = vec3(0);
	vec3 sum;
	//vec2 offset = screen_uv * 2.0 - 1.0;
	vec2 offset = vec2(0.5) - screen_uv;
	for(int i = 0; i < chromaticLevels; i++){
		float t = 2.0*float(i)/float(chromaticLevels-1); // range 0.0->2.0
		vec3 slice = chromatic_slice(t);
		slice = max(slice, 0.0);
		sum += slice;
		vec2 slice_offset = (t-1.0)*chromaticSpread*offset;
		result.rgb += slice * texture(screen_texture, screen_uv + slice_offset).rgb;
	}
	result.rgb /= sum;
	return result;
}

vec2 warp(vec2 inpt){
	vec2 delta = inpt - 0.5;
	float delta2 = dot(delta.xy, delta.xy);
	float delta4 = delta2 * delta2;
	float delta_offset = delta4 * warp_amount;
	
	vec2 warped = inpt + delta * delta_offset;
	return (warped - 0.5) / mix(1.0,1.2,warp_amount/5.0) + 0.5;
}

void vertex() {
	
}

void fragment() {
	screen_uv = warp(SCREEN_UV);
	uv = warp(UV);
	texture_pixel_size = TEXTURE_PIXEL_SIZE;
	screenColor = textureLod(screen_texture, screen_uv, 0.0);
	
	float dist = distance(vec2(0.5) * chromaticRadiusScale,uv * chromaticRadiusScale);
	
	vec3 ditherColor = calcDitherAndColorLimit();
	vec3 chromaticColor = calcChromaticAbberation();
	vec3 result = (ditherColor + chromaticColor) / 2.0;
	
	COLOR.rgb = mix(ditherColor,result,dist);
}







