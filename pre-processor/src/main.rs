extern crate bmp;
use glob::glob;
fn main() {

    // Iterate over all frames in frames directory.
    for entry in glob("../frames/*.bmp").expect("Failed to read glob pattern.") {
        match entry {
            Ok(path) => {

                // Open the image.
                let img = bmp::open(path).unwrap();

                let mut black = true;
                let mut counter = 0;
                let mut numbers: Vec<u32> = Vec::new();

                // Iterate over each pixel in the image.
                for (x, y) in img.coordinates() {
                    let is_black = img.get_pixel(x, y).r < 128;

                    // When the colour changes, save the counter to our numbers before resetting it.
                    if is_black != black {
                        black = is_black;
                        numbers.push(counter.clone());
                        counter = 0;
                    }
                    counter += 1;
                }
                // The loop has finished, save whatever was in the counter.
                numbers.push(counter);

                // Encode the current frame using "bad encode" xD
                bad_encode(numbers);
            }
            Err(_) => {}
        }
    }
}

fn bad_encode(numbers: Vec<u32>) {
    const OFFSET: u32 = 0x00B0;
    let n: String = numbers
        .into_iter()
        .map(|x| char::from_u32(x + OFFSET).expect("Failed to encode"))
        .collect();
    println!("{}", n);
}
